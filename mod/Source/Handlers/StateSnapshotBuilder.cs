using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RimworldGM.Util;
using RimWorld;
using Verse;

namespace RimworldGM.Handlers
{
    public static class StateSnapshotBuilder
    {
        public static bool TryBuild(bool includeColonists, bool includeResources, out string json, out string error)
        {
            json = null;
            error = null;

            var map = Find.CurrentMap;
            if (map == null)
            {
                error = "NO_COLONY_LOADED";
                return false;
            }

            var sb = new StringBuilder(2048);
            sb.Append('{');

            AppendColony(sb, map);
            sb.Append(',');

            if (includeColonists)
            {
                AppendColonists(sb, map);
            }
            else
            {
                sb.Append("\"colonists\":[]");
            }

            sb.Append(',');

            if (includeResources)
            {
                AppendResources(sb, map);
            }
            else
            {
                sb.Append("\"resources\":{}");
            }

            sb.Append(',');
            AppendThreats(sb, map);

            sb.Append('}');
            json = sb.ToString();
            return true;
        }

        private static void AppendColony(StringBuilder sb, Map map)
        {
            var day = GenDate.DayOfYear(Find.TickManager.TicksAbs, 0f);
            var quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, 0f).Label();
            var season = GenLocalDate.Season(map).Label();
            var wealth = map.wealthWatcher != null ? (int)map.wealthWatcher.WealthTotal : 0;

            sb.Append("\"colony\":{");
            sb.Append("\"name\":").Append(Json.Quote("Colony")).Append(',');
            sb.Append("\"wealth\":").Append(Json.Number(wealth)).Append(',');
            sb.Append("\"day\":").Append(Json.Number(day)).Append(',');
            sb.Append("\"season\":").Append(Json.Quote(season)).Append(',');
            sb.Append("\"quadrum\":").Append(Json.Quote(quadrum));
            sb.Append('}');
        }

        private static void AppendColonists(StringBuilder sb, Map map)
        {
            sb.Append("\"colonists\":[");

            var colonists = map.mapPawns != null ? map.mapPawns.FreeColonistsSpawned : new List<Pawn>();
            for (var i = 0; i < colonists.Count; i++)
            {
                var p = colonists[i];
                if (i > 0)
                {
                    sb.Append(',');
                }

                var mood = p.needs != null && p.needs.mood != null ? (int)(p.needs.mood.CurLevelPercentage * 100f) : 0;
                var health = p.health != null ? (int)(p.health.summaryHealth.SummaryHealthPercent * 100f) : 0;
                var activity = p.CurJobDef != null ? p.CurJobDef.label : "Idle";

                sb.Append('{');
                sb.Append("\"name\":").Append(Json.Quote(p.LabelShortCap)).Append(',');
                sb.Append("\"mood\":").Append(Json.Number(mood)).Append(',');
                sb.Append("\"health\":").Append(Json.Number(health)).Append(',');
                sb.Append("\"current_activity\":").Append(Json.Quote(activity)).Append(',');

                sb.Append("\"skills\":{");
                sb.Append("\"shooting\":").Append(Json.Number(GetSkillLevel(p, SkillDefOf.Shooting))).Append(',');
                sb.Append("\"melee\":").Append(Json.Number(GetSkillLevel(p, SkillDefOf.Melee))).Append(',');
                sb.Append("\"cooking\":").Append(Json.Number(GetSkillLevel(p, SkillDefOf.Cooking)));
                sb.Append("},");

                sb.Append("\"traits\":[");
                if (p.story != null && p.story.traits != null && p.story.traits.allTraits != null)
                {
                    for (var t = 0; t < p.story.traits.allTraits.Count; t++)
                    {
                        if (t > 0)
                        {
                            sb.Append(',');
                        }
                        sb.Append(Json.Quote(p.story.traits.allTraits[t].LabelCap));
                    }
                }
                sb.Append(']');
                sb.Append('}');
            }

            sb.Append(']');
        }

        private static int GetSkillLevel(Pawn pawn, SkillDef skillDef)
        {
            if (pawn == null || pawn.skills == null || skillDef == null)
            {
                return 0;
            }

            var record = pawn.skills.GetSkill(skillDef);
            return record != null ? record.Level : 0;
        }

        private static void AppendResources(StringBuilder sb, Map map)
        {
            var silver = GetResourceCount(map, ThingDefOf.Silver);
            var medicine = GetResourceCount(map, ThingDefOf.MedicineIndustrial);
            var components = GetResourceCount(map, ThingDefOf.ComponentIndustrial);
            var food = GetResourceCount(map, ThingDefOf.MealSimple) + GetResourceCount(map, ThingDefOf.Pemmican);

            sb.Append("\"resources\":{");
            sb.Append("\"silver\":").Append(Json.Number(silver)).Append(',');
            sb.Append("\"food\":").Append(Json.Number(food)).Append(',');
            sb.Append("\"medicine\":").Append(Json.Number(medicine)).Append(',');
            sb.Append("\"components\":").Append(Json.Number(components));
            sb.Append('}');
        }

        private static int GetResourceCount(Map map, ThingDef thingDef)
        {
            if (map == null || map.resourceCounter == null || thingDef == null)
            {
                return 0;
            }

            return map.resourceCounter.GetCount(thingDef);
        }

        private static void AppendThreats(StringBuilder sb, Map map)
        {
            var hostiles = map.attackTargetsCache != null && map.attackTargetsCache.TargetsHostileToColony != null
                ? map.attackTargetsCache.TargetsHostileToColony.Count
                : 0;

            sb.Append("\"threats\":{");
            sb.Append("\"active_raids\":").Append(Json.Number(hostiles > 0 ? 1 : 0)).Append(',');
            sb.Append("\"nearby_enemies\":").Append(Json.Bool(hostiles > 0)).Append(',');
            sb.Append("\"toxic_fallout\":").Append(Json.Bool(GameConditionActive(map, "ToxicFallout")));
            sb.Append('}');
        }

        private static bool GameConditionActive(Map map, string conditionDefName)
        {
            if (map == null || map.GameConditionManager == null)
            {
                return false;
            }

            var conditions = map.GameConditionManager.ActiveConditions;
            for (var i = 0; i < conditions.Count; i++)
            {
                var def = conditions[i].def;
                if (def != null && def.defName == conditionDefName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
