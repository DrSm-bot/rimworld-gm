using System;
using System.Net;
using System.Text;

namespace RimworldGM.Http
{
    /// <summary>
    /// Writes JSON responses to HttpListenerResponse.
    /// </summary>
    public static class HttpResponseWriter
    {
        public static void WriteJson(HttpListenerResponse response, int statusCode, string json)
        {
            if (response == null)
            {
                return;
            }

            var payload = json ?? "{}";
            var buffer = Encoding.UTF8.GetBytes(payload);

            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.LongLength;

            using (var stream = response.OutputStream)
            {
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        public static void WriteNotFound(HttpListenerResponse response)
        {
            WriteJson(response, 404, "{\"success\":false,\"error\":\"NOT_FOUND\",\"message\":\"Route not found\"}");
        }

        public static void WriteServerError(HttpListenerResponse response)
        {
            WriteJson(response, 500, "{\"success\":false,\"error\":\"INTERNAL_ERROR\",\"message\":\"Internal server error\"}");
        }
    }
}
