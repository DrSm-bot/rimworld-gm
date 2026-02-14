using System;
using System.Collections.Generic;

namespace RimworldGM.Util
{
    public class RateLimiter
    {
        private readonly object _sync = new object();
        private readonly Dictionary<string, Queue<DateTime>> _hits = new Dictionary<string, Queue<DateTime>>();

        public bool TryEnter(string key, int maxPerMinute)
        {
            if (maxPerMinute <= 0)
            {
                return true;
            }

            var id = string.IsNullOrEmpty(key) ? "global" : key;
            var now = DateTime.UtcNow;
            var cutoff = now.AddMinutes(-1);

            lock (_sync)
            {
                Queue<DateTime> queue;
                if (!_hits.TryGetValue(id, out queue))
                {
                    queue = new Queue<DateTime>();
                    _hits[id] = queue;
                }

                while (queue.Count > 0 && queue.Peek() < cutoff)
                {
                    queue.Dequeue();
                }

                if (queue.Count >= maxPerMinute)
                {
                    return false;
                }

                queue.Enqueue(now);
                return true;
            }
        }
    }
}
