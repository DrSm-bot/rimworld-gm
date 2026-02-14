using RimworldGM.Util;
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
            : base(game)
        {
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
                case GameCommandKind.Event:
                case GameCommandKind.Message:
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
    }
}
