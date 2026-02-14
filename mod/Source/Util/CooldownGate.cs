using System;
using System.Collections.Generic;

namespace RimworldGM.Util
{
    public static class CooldownGate
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, DateTime> LastByKey = new Dictionary<string, DateTime>();

        public static bool TryEnter(string key, int cooldownSeconds, out int retryAfterSeconds)
        {
            retryAfterSeconds = 0;
            if (string.IsNullOrEmpty(key))
            {
                return true;
            }

            var now = DateTime.UtcNow;
            lock (Sync)
            {
                DateTime last;
                if (LastByKey.TryGetValue(key, out last))
                {
                    var elapsed = (now - last).TotalSeconds;
                    if (elapsed < cooldownSeconds)
                    {
                        retryAfterSeconds = Math.Max(1, cooldownSeconds - (int)elapsed);
                        return false;
                    }
                }

                LastByKey[key] = now;
                return true;
            }
        }
    }
}
