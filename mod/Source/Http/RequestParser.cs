using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RimworldGM.Http
{
    public static class RequestParser
    {
        public static bool IsGet(HttpListenerRequest request, string path)
        {
            return IsMethodPath(request, "GET", path);
        }

        public static bool IsPost(HttpListenerRequest request, string path)
        {
            return IsMethodPath(request, "POST", path);
        }

        public static bool GetQueryBool(HttpListenerRequest request, string key, bool defaultValue)
        {
            if (request == null || request.QueryString == null)
            {
                return defaultValue;
            }

            var raw = request.QueryString[key];
            if (string.IsNullOrEmpty(raw))
            {
                return defaultValue;
            }

            return raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public static string ReadBody(HttpListenerRequest request, int maxBytes)
        {
            if (request == null || request.InputStream == null || !request.HasEntityBody)
            {
                return string.Empty;
            }

            if (maxBytes <= 0)
            {
                maxBytes = 16384;
            }

            var encoding = request.ContentEncoding ?? Encoding.UTF8;
            var buffer = new byte[4096];
            var ms = new MemoryStream();
            var total = 0;

            while (true)
            {
                var read = request.InputStream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                total += read;
                if (total > maxBytes)
                {
                    throw new InvalidOperationException("REQUEST_BODY_TOO_LARGE");
                }

                ms.Write(buffer, 0, read);
            }

            return encoding.GetString(ms.ToArray());
        }

        public static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            {
                return null;
            }

            var pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"])*)\"";
            var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            return Regex.Unescape(match.Groups["v"].Value);
        }

        public static int? ExtractJsonInt(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            {
                return null;
            }

            var pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*(?<v>-?[0-9]+)";
            var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            int value;
            if (!int.TryParse(match.Groups["v"].Value, out value))
            {
                return null;
            }

            return value;
        }

        private static bool IsMethodPath(HttpListenerRequest request, string method, string path)
        {
            if (request == null)
            {
                return false;
            }

            if (!string.Equals(request.HttpMethod, method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var actualPath = request.Url != null ? request.Url.AbsolutePath : string.Empty;
            return string.Equals(actualPath, path, StringComparison.OrdinalIgnoreCase);
        }
    }
}
