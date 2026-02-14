using RimworldGM.Util;
using RimWorld;
using Verse;

namespace RimworldGM.Handlers
{
    public static class EventHandler
    {
        public static bool TryTrigger(string eventType, int points, out string error)
        {
            error = null;
            var map = Find.CurrentMap;
            if (map == null)
            {
                error = "NO_COLONY_LOADED";
                return false;
            }

            var normalized = (eventType ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrEmpty(normalized))
            {
                error = "INVALID_EVENT";
                return false;
            }

            var incident = ResolveIncident(normalized);
            if (incident == null)
            {
                error = "INVALID_EVENT";
                return false;
            }

            int retryAfter;
            if (!CooldownGate.TryEnter("event:" + normalized, 30, out retryAfter))
            {
                error = "COOLDOWN_ACTIVE";
                return false;
            }

            try
            {
                var parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
                if (points > 0)
                {
                    parms.points = points;
                }

                var ok = incident.Worker.TryExecute(parms);
                if (!ok)
                {
                    error = "EVENT_FAILED";
                }
                return ok;
            }
            catch
            {
                error = "EVENT_FAILED";
                return false;
            }
        }

        private static IncidentDef ResolveIncident(string eventType)
        {
            switch (eventType)
            {
                case "raid":
                    return IncidentDefOf.RaidEnemy;
                case "solar_flare":
                    return IncidentDef.Named("SolarFlare");
                case "cold_snap":
                    return IncidentDef.Named("ColdSnap");
                case "manhunter":
                    return IncidentDef.Named("ManhunterPack");
                case "cargo_pod":
                    return IncidentDef.Named("ResourcePodCrash");
                default:
                    return null;
            }
        }
    }
}
