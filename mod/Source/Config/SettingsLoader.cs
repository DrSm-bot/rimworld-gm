using System;
using System.IO;
using System.Xml;
using Verse;

namespace RimworldGM.Config
{
    public static class SettingsLoader
    {
        public static RimworldGMSettings Current = new RimworldGMSettings();

        public static RimworldGMSettings Load()
        {
            var settings = new RimworldGMSettings();
            var path = FindSettingsPath();

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    var doc = new XmlDocument();
                    doc.Load(path);

                    settings.Network.BindAddress = ReadString(doc, "/RimworldGMSettings/network/bindAddress", settings.Network.BindAddress);
                    settings.Network.Port = ReadInt(doc, "/RimworldGMSettings/network/port", settings.Network.Port);
                    settings.Network.AuthToken = ReadString(doc, "/RimworldGMSettings/network/authToken", settings.Network.AuthToken);
                    settings.Network.AllowedCidrs = ReadString(doc, "/RimworldGMSettings/network/allowedCidrs", settings.Network.AllowedCidrs);
                    settings.Network.AllowLan = ReadBool(doc, "/RimworldGMSettings/network/allowLan", settings.Network.AllowLan);

                    settings.Security.RequireTokenForLan = ReadBool(doc, "/RimworldGMSettings/security/requireTokenForLan", settings.Security.RequireTokenForLan);
                    settings.Security.MaxRequestsPerMinute = ReadInt(doc, "/RimworldGMSettings/security/maxRequestsPerMinute", settings.Security.MaxRequestsPerMinute);
                    settings.Security.MaxRequestBodyBytes = ReadInt(doc, "/RimworldGMSettings/security/maxRequestBodyBytes", settings.Security.MaxRequestBodyBytes);
                    settings.Security.EnableDangerousEvents = ReadBool(doc, "/RimworldGMSettings/security/enableDangerousEvents", settings.Security.EnableDangerousEvents);

                    settings.Ui.ShowLanWarningOnce = ReadBool(doc, "/RimworldGMSettings/ui/showLanWarningOnce", settings.Ui.ShowLanWarningOnce);
                    settings.Ui.ShowStartupSummary = ReadBool(doc, "/RimworldGMSettings/ui/showStartupSummary", settings.Ui.ShowStartupSummary);
                }
                catch (Exception ex)
                {
                    Log.Warning("[RimworldGM] Failed to parse settings XML, using defaults: " + ex.Message);
                }
            }
            else
            {
                Log.Message("[RimworldGM] Settings.xml not found, using defaults.");
            }

            ValidateAndNormalize(settings);
            Current = settings;
            return settings;
        }

        public static string FindSettingsPath()
        {
            var cwdPath = Path.Combine(Environment.CurrentDirectory, "Mods", "RimworldGM", "Config", "Settings.xml");
            if (File.Exists(cwdPath))
            {
                return cwdPath;
            }

            var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "RimworldGM", "Config", "Settings.xml");
            if (File.Exists(basePath))
            {
                return basePath;
            }

            return cwdPath;
        }

        private static void ValidateAndNormalize(RimworldGMSettings s)
        {
            if (s.Network.Port < 1024 || s.Network.Port > 65535)
            {
                s.Network.Port = 18800;
            }

            if (s.Security.MaxRequestsPerMinute < 10)
            {
                s.Security.MaxRequestsPerMinute = 10;
            }

            if (s.Security.MaxRequestBodyBytes < 1024)
            {
                s.Security.MaxRequestBodyBytes = 1024;
            }

            if (string.IsNullOrEmpty(s.Network.BindAddress))
            {
                s.Network.BindAddress = "127.0.0.1";
            }

            var isLan = !(s.Network.BindAddress == "127.0.0.1" || s.Network.BindAddress == "localhost");
            if (isLan)
            {
                var invalid = !s.Network.AllowLan || (s.Security.RequireTokenForLan && string.IsNullOrEmpty(s.Network.AuthToken));
                if (invalid)
                {
                    Log.Warning("[RimworldGM] LAN mode requested but invalid config; falling back to localhost.");
                    s.Network.BindAddress = "127.0.0.1";
                    s.Network.AllowLan = false;
                }
            }
        }

        private static string ReadString(XmlDocument doc, string xpath, string fallback)
        {
            var node = doc.SelectSingleNode(xpath);
            return node != null ? (node.InnerText ?? fallback).Trim() : fallback;
        }

        private static int ReadInt(XmlDocument doc, string xpath, int fallback)
        {
            var raw = ReadString(doc, xpath, fallback.ToString());
            int v;
            return int.TryParse(raw, out v) ? v : fallback;
        }

        private static bool ReadBool(XmlDocument doc, string xpath, bool fallback)
        {
            var raw = ReadString(doc, xpath, fallback ? "true" : "false");
            return raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
