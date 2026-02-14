namespace RimworldGM.Config
{
    public class RimworldGMSettings
    {
        public NetworkSettings Network = new NetworkSettings();
        public SecuritySettings Security = new SecuritySettings();
        public UiSettings Ui = new UiSettings();

        public class NetworkSettings
        {
            public string BindAddress = "127.0.0.1";
            public int Port = 18800;
            public string AuthToken = string.Empty;
            public string AllowedCidrs = string.Empty;
            public bool AllowLan = false;
        }

        public class SecuritySettings
        {
            public bool RequireTokenForLan = true;
            public int MaxRequestsPerMinute = 60;
            public int MaxRequestBodyBytes = 16384;
            public bool EnableDangerousEvents = false;
        }

        public class UiSettings
        {
            public bool ShowLanWarningOnce = true;
            public bool ShowStartupSummary = true;
        }

        public bool IsLanMode
        {
            get
            {
                return !(Network.BindAddress == "127.0.0.1" || Network.BindAddress == "localhost");
            }
        }
    }
}
