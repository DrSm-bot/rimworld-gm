using RimworldGM.Handlers;
using RimworldGM.Util;
using RimWorld;
using Verse;

namespace RimworldGM.Core
{
    /// <summary>
    /// Main-thread command pump.
    /// </summary>
    public class GameComponent_RimworldGM : GameComponent
    {
        private const int MaxCommandsPerTick = 10;

        public GameComponent_RimworldGM(Game game)
        {
            // Note: Base class constructor signature varies by Rimworld version
            // Parameterless for compatibility
        }

        public override void GameComponentTick()
        {
            CommandBus.Dispatcher.CleanupStale(30);

            for (var i = 0; i < MaxCommandsPerTick; i++)
            {
                GameCommand command;
                if (!CommandBus.Dispatcher.TryDequeue(out command))
                {
                    break;
                }

                if (command.Cancelled)
                {
                    continue;
                }

                var result = Execute(command);
                CommandBus.Dispatcher.SetResult(command.RequestId, result);
            }
        }

        private CommandResult Execute(GameCommand command)
        {
            if (command == null)
            {
                return CommandResult.Fail("INVALID_COMMAND");
            }

            switch (command.Kind)
            {
                case GameCommandKind.Health:
                    return BuildHealthResult(command);
                case GameCommandKind.State:
                    return BuildStateResult(command);
                case GameCommandKind.Event:
                    return BuildEventResult(command);
                case GameCommandKind.Message:
                    return BuildMessageResult(command);
                default:
                    return CommandResult.Fail("NOT_IMPLEMENTED");
            }
        }

        private CommandResult BuildHealthResult(GameCommand command)
        {
            string uptime;
            if (!command.Args.TryGetValue("uptime_seconds", out uptime) || string.IsNullOrEmpty(uptime))
            {
                uptime = "0";
            }

            var colonyLoaded = Current.Game != null && Find.CurrentMap != null;
            var queueDepth = CommandBus.Dispatcher.QueueDepth;

            var healthJson =
                "{" +
                "\"status\":" + Json.Quote("ok") + "," +
                "\"game_running\":" + Json.Bool(true) + "," +
                "\"colony_loaded\":" + Json.Bool(colonyLoaded) + "," +
                "\"mod_version\":" + Json.Quote(global::RimworldGM.RimworldGM.VERSION) + "," +
                "\"queue_depth\":" + Json.Number(queueDepth) + "," +
                "\"uptime_seconds\":" + uptime +
                "}";

            return CommandResult.Ok(healthJson);
        }

        private CommandResult BuildStateResult(GameCommand command)
        {
            var includeColonists = GetBoolArg(command, "include_colonists", true);
            var includeResources = GetBoolArg(command, "include_resources", true);

            string json;
            string error;
            if (!StateSnapshotBuilder.TryBuild(includeColonists, includeResources, out json, out error))
            {
                return CommandResult.Fail(error ?? "EVENT_FAILED");
            }

            return CommandResult.Ok(json);
        }

        private CommandResult BuildEventResult(GameCommand command)
        {
            var eventType = GetArg(command, "event_type", string.Empty);
            var points = GetIntArg(command, "points", 500);

            string error;
            if (!EventHandler.TryTrigger(eventType, points, out error))
            {
                return CommandResult.Fail(error ?? "EVENT_FAILED");
            }

            var eventId = GetArg(command, "event_id", command.RequestId);
            var json = "{" +
                       "\"success\":true," +
                       "\"message\":" + Json.Quote("Event triggered successfully") + "," +
                       "\"event_id\":" + Json.Quote(eventId) +
                       "}";
            return CommandResult.Ok(json);
        }

        private CommandResult BuildMessageResult(GameCommand command)
        {
            if (Find.CurrentMap == null)
            {
                return CommandResult.Fail("NO_COLONY_LOADED");
            }

            var text = GetArg(command, "text", string.Empty);
            var style = GetArg(command, "type", "info");

            if (string.IsNullOrEmpty(text))
            {
                return CommandResult.Fail("INVALID_REQUEST");
            }

            var ok = MessageHandler.Send(text, style);
            if (!ok)
            {
                return CommandResult.Fail("EVENT_FAILED");
            }

            return CommandResult.Ok("{\"success\":true}");
        }

        private static string GetArg(GameCommand command, string key, string defaultValue)
        {
            if (command == null || command.Args == null)
            {
                return defaultValue;
            }

            string value;
            if (command.Args.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return defaultValue;
        }

        private static bool GetBoolArg(GameCommand command, string key, bool defaultValue)
        {
            var raw = GetArg(command, key, defaultValue ? "true" : "false");
            return raw == "1" || raw.Equals("true", System.StringComparison.OrdinalIgnoreCase);
        }

        private static int GetIntArg(GameCommand command, string key, int defaultValue)
        {
            var raw = GetArg(command, key, defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            int value;
            return int.TryParse(raw, out value) ? value : defaultValue;
        }
    }
}
