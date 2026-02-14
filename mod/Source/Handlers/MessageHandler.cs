using RimWorld;
using Verse;

namespace RimworldGM.Handlers
{
    public static class MessageHandler
    {
        public static bool Send(string text, string style)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var normalized = (style ?? "info").ToLowerInvariant();

            if (normalized == "dramatic")
            {
                Find.LetterStack.ReceiveLetter("Rimworld GM", text, LetterDefOf.NeutralEvent, null);
                return true;
            }

            MessageTypeDef typeDef;
            switch (normalized)
            {
                case "positive":
                    typeDef = MessageTypeDefOf.PositiveEvent;
                    break;
                case "negative":
                    typeDef = MessageTypeDefOf.NegativeEvent;
                    break;
                case "info":
                default:
                    typeDef = MessageTypeDefOf.NeutralEvent;
                    break;
            }

            Messages.Message(text, typeDef, true);
            return true;
        }
    }
}
