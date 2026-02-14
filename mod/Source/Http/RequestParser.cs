using System;
using System.Net;

namespace RimworldGM.Http
{
    /// <summary>
    /// Minimal request parser for Phase 1 PR #2.
    /// Focuses on route/method matching for /health.
    /// </summary>
    public static class RequestParser
    {
        public static bool IsHealthRequest(HttpListenerRequest request)
        {
            if (request == null)
            {
                return false;
            }

            if (!string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var path = request.Url != null ? request.Url.AbsolutePath : string.Empty;
            return string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase);
        }
    }
}
