using System;
using System.Collections.Generic;
using System.Threading;

namespace RimworldGM.Core
{
    /// <summary>
    /// Thread-safe command queue + result registry.
    /// Lock-based for runtime compatibility.
    /// </summary>
    public class CommandDispatcher
    {
        private class PendingEntry
        {
            public DateTime CreatedUtc;
            public CommandResult Result;
        }

        private readonly Queue<GameCommand> _queue = new Queue<GameCommand>();
        private readonly Dictionary<string, PendingEntry> _pending = new Dictionary<string, PendingEntry>();
        private readonly object _sync = new object();

        public string Enqueue(GameCommand command)
        {
            if (command == null)
            {
                return null;
            }

            lock (_sync)
            {
                _queue.Enqueue(command);
                _pending[command.RequestId] = new PendingEntry
                {
                    CreatedUtc = DateTime.UtcNow,
                    Result = null
                };
                return command.RequestId;
            }
        }

        public bool TryDequeue(out GameCommand command)
        {
            lock (_sync)
            {
                if (_queue.Count > 0)
                {
                    command = _queue.Dequeue();
                    return true;
                }
            }

            command = null;
            return false;
        }

        public void SetResult(string requestId, CommandResult result)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                return;
            }

            lock (_sync)
            {
                PendingEntry entry;
                if (_pending.TryGetValue(requestId, out entry))
                {
                    entry.Result = result;
                }
            }
        }

        public bool TryGetResult(string requestId, out CommandResult result)
        {
            lock (_sync)
            {
                PendingEntry entry;
                if (_pending.TryGetValue(requestId, out entry) && entry.Result != null)
                {
                    result = entry.Result;
                    _pending.Remove(requestId);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public bool WaitForResult(string requestId, int timeoutMs, out CommandResult result)
        {
            var started = DateTime.UtcNow;
            while ((DateTime.UtcNow - started).TotalMilliseconds < timeoutMs)
            {
                if (TryGetResult(requestId, out result))
                {
                    return true;
                }

                Thread.Sleep(10);
            }

            result = null;
            return false;
        }

        public void CleanupStale(int maxAgeSeconds)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-maxAgeSeconds);
            var keysToRemove = new List<string>();

            lock (_sync)
            {
                foreach (var kv in _pending)
                {
                    if (kv.Value.CreatedUtc < cutoff)
                    {
                        keysToRemove.Add(kv.Key);
                    }
                }

                for (var i = 0; i < keysToRemove.Count; i++)
                {
                    _pending.Remove(keysToRemove[i]);
                }
            }
        }

        public int QueueDepth
        {
            get
            {
                lock (_sync)
                {
                    return _queue.Count;
                }
            }
        }
    }

    public static class CommandBus
    {
        public static readonly CommandDispatcher Dispatcher = new CommandDispatcher();
    }
}
