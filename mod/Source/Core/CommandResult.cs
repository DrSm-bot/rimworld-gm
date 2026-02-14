namespace RimworldGM.Core
{
    public class CommandResult
    {
        public bool Success;
        public string Error;
        public object Data;

        public static CommandResult Ok(object data)
        {
            return new CommandResult
            {
                Success = true,
                Error = null,
                Data = data
            };
        }

        public static CommandResult Fail(string error)
        {
            return new CommandResult
            {
                Success = false,
                Error = error,
                Data = null
            };
        }
    }
}
