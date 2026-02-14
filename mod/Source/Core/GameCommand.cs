using System;
using System.Collections.Generic;

namespace RimworldGM.Core
{
    public enum GameCommandKind
    {
        Health,
        State,
        Event,
        Message
    }

    public class GameCommand
    {
        public string RequestId;
        public GameCommandKind Kind;
        public Dictionary<string, string> Args;
        public volatile bool Cancelled;

        public GameCommand(GameCommandKind kind, Dictionary<string, string> args)
        {
            RequestId = Guid.NewGuid().ToString("N");
            Kind = kind;
            Args = args ?? new Dictionary<string, string>();
            Cancelled = false;
        }
    }
}
