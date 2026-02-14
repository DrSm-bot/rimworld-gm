using System;
using Verse;
using RimworldGM.Http;

namespace RimworldGM
{
    /// <summary>
    /// Main mod bootstrap for Rimworld Game Master.
    /// Initializes and owns the HTTP server lifecycle.
    /// </summary>
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
                _server = new HttpServer(DEFAULT_PORT);
                _server.Start();

                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                AppDomain.CurrentDomain.DomainUnload += OnProcessExit;

                Log.Message("[RimworldGM] Ready. API available at http://localhost:" + DEFAULT_PORT);
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
