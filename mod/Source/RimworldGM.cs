using System;
using RimworldGM.Config;
using RimworldGM.Http;
using Verse;

namespace RimworldGM
{
    [StaticConstructorOnStartup]
    public static class RimworldGM
    {
        public const string VERSION = "0.1.0";
        public const int DEFAULT_PORT = 18800;

        private static HttpServer _server;

        static RimworldGM()
        {
            Log.Message("[RimworldGM] Initializing v" + VERSION);

            try
            {
                var settings = SettingsLoader.Load();

                _server = new HttpServer(settings);
                _server.Start();

                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                AppDomain.CurrentDomain.DomainUnload += OnProcessExit;

                if (settings.Ui.ShowStartupSummary)
                {
                    Log.Message("[RimworldGM] Startup: bind=" + settings.Network.BindAddress +
                                " port=" + settings.Network.Port +
                                " lan=" + (settings.IsLanMode ? "true" : "false") +
                                " tokenConfigured=" + (!string.IsNullOrEmpty(settings.Network.AuthToken) ? "yes" : "no"));
                }

                if (settings.IsLanMode && settings.Ui.ShowLanWarningOnce)
                {
                    Log.Warning("[RimworldGM] LAN mode enabled. API is reachable from network. Keep token secret.");
                }

                Log.Message("[RimworldGM] Ready. API available at http://" + settings.Network.BindAddress + ":" + settings.Network.Port);
            }
            catch (Exception ex)
            {
                Log.Error("[RimworldGM] Failed to start HTTP server: " + ex);
            }
        }

        private static void OnProcessExit(object sender, EventArgs args)
        {
            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }
        }
    }
}
