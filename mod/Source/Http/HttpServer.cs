using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using RimworldGM.Config;
using RimworldGM.Core;
using RimworldGM.Util;
using Verse;

namespace RimworldGM.Http
{
    public class HttpServer
    {
        private readonly RimworldGMSettings _settings;
        private readonly RateLimiter _rateLimiter = new RateLimiter();
        private HttpListener _listener;
        private Thread _listenerThread;
        private volatile bool _running;
        private DateTime _startedAtUtc;

        public HttpServer(RimworldGMSettings settings)
        {
            _settings = settings ?? new RimworldGMSettings();
        }

        public void Start()
        {
            if (_running)
            {
                return;
            }

            _listener = new HttpListener();
            var host = _settings.Network.BindAddress == "0.0.0.0" ? "+" : _settings.Network.BindAddress;
            _listener.Prefixes.Add("http://" + host + ":" + _settings.Network.Port + "/");
            _listener.Start();

            _startedAtUtc = DateTime.UtcNow;
            _running = true;

            _listenerThread = new Thread(ListenLoop);
            _listenerThread.Name = "RimworldGM.HttpListener";
            _listenerThread.IsBackground = true;
            _listenerThread.Start();
        }

        public void Stop()
        {
            _running = false;

            try
            {
                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[RimworldGM] HTTP stop warning: " + ex.Message);
            }

            try
            {
                if (_listener != null)
                {
                    _listener.Close();
                }
            }
            catch
            {
            }

            _listener = null;
        }

        private void ListenLoop()
        {
            while (_running)
            {
                HttpListenerContext context = null;
                try
                {
                    if (_listener == null || !_listener.IsListening)
                    {
                        Thread.Sleep(25);
                        continue;
                    }

                    context = _listener.GetContext();
                    HandleRequest(context);
                }
                catch (HttpListenerException)
                {
                    if (_running)
                    {
                        Log.Warning("[RimworldGM] HTTP listener interrupted while running.");
                    }
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error("[RimworldGM] HTTP loop error: " + ex);
                    if (context != null)
                    {
                        try
                        {
                            HttpResponseWriter.WriteServerError(context.Response);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;

            if (!_rateLimiter.TryEnter("global", _settings.Security.MaxRequestsPerMinute))
            {
                HttpResponseWriter.WriteApiError(context.Response, 429, "RATE_LIMITED", "Too many requests");
                return;
            }

            if (RequiresAuth(request) && !IsAuthorized(request))
            {
                HttpResponseWriter.WriteApiError(context.Response, 401, "UNAUTHORIZED", "Missing or invalid token");
                return;
            }

            if ((RequestParser.IsPost(request, "/event") || RequestParser.IsPost(request, "/message"))
                && request.ContentLength64 > _settings.Security.MaxRequestBodyBytes)
            {
                HttpResponseWriter.WriteApiError(context.Response, 400, "INVALID_REQUEST", "Request body too large");
                return;
            }

            if (RequestParser.IsGet(request, "/health"))
            {
                var args = new Dictionary<string, string>();
                args["uptime_seconds"] = UptimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
                DispatchCommand(context, new GameCommand(GameCommandKind.Health, args));
                return;
            }

            if (RequestParser.IsGet(request, "/state"))
            {
                var args = new Dictionary<string, string>();
                args["include_colonists"] = RequestParser.GetQueryBool(request, "include_colonists", true) ? "true" : "false";
                args["include_resources"] = RequestParser.GetQueryBool(request, "include_resources", true) ? "true" : "false";
                DispatchCommand(context, new GameCommand(GameCommandKind.State, args));
                return;
            }

            if (RequestParser.IsPost(request, "/event"))
            {
                string body;
                try
                {
                    body = RequestParser.ReadBody(request, _settings.Security.MaxRequestBodyBytes);
                }
                catch (InvalidOperationException)
                {
                    HttpResponseWriter.WriteApiError(context.Response, 400, "INVALID_REQUEST", "Request body too large");
                    return;
                }

                var eventType = RequestParser.ExtractJsonString(body, "event_type");
                if (string.IsNullOrEmpty(eventType))
                {
                    HttpResponseWriter.WriteApiError(context.Response, 400, "INVALID_REQUEST", "event_type is required");
                    return;
                }

                var points = RequestParser.ExtractJsonInt(body, "points") ?? 500;
                var args = new Dictionary<string, string>();
                args["event_type"] = eventType;
                args["points"] = points.ToString(System.Globalization.CultureInfo.InvariantCulture);
                args["event_id"] = Guid.NewGuid().ToString("N");

                DispatchCommand(context, new GameCommand(GameCommandKind.Event, args));
                return;
            }

            if (RequestParser.IsPost(request, "/message"))
            {
                string body;
                try
                {
                    body = RequestParser.ReadBody(request, _settings.Security.MaxRequestBodyBytes);
                }
                catch (InvalidOperationException)
                {
                    HttpResponseWriter.WriteApiError(context.Response, 400, "INVALID_REQUEST", "Request body too large");
                    return;
                }

                var text = RequestParser.ExtractJsonString(body, "text");
                if (string.IsNullOrEmpty(text))
                {
                    HttpResponseWriter.WriteApiError(context.Response, 400, "INVALID_REQUEST", "text is required");
                    return;
                }

                var style = RequestParser.ExtractJsonString(body, "type") ?? "info";
                var args = new Dictionary<string, string>();
                args["text"] = text;
                args["type"] = style;

                DispatchCommand(context, new GameCommand(GameCommandKind.Message, args));
                return;
            }

            HttpResponseWriter.WriteNotFound(context.Response);
        }

        private bool RequiresAuth(HttpListenerRequest request)
        {
            return _settings.IsLanMode;
        }

        private bool IsAuthorized(HttpListenerRequest request)
        {
            if (string.IsNullOrEmpty(_settings.Network.AuthToken))
            {
                return false;
            }

            var auth = request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer "))
            {
                var token = auth.Substring("Bearer ".Length).Trim();
                return token == _settings.Network.AuthToken;
            }

            var legacyToken = request.Headers["X-RimworldGM-Token"];
            if (!string.IsNullOrEmpty(legacyToken))
            {
                return legacyToken == _settings.Network.AuthToken;
            }

            return false;
        }

        private void DispatchCommand(HttpListenerContext context, GameCommand command)
        {
            var requestId = CommandBus.Dispatcher.Enqueue(command);
            if (string.IsNullOrEmpty(requestId))
            {
                HttpResponseWriter.WriteApiError(context.Response, 503, "MOD_NOT_READY", "Command dispatcher unavailable");
                return;
            }

            CommandResult result;
            if (!CommandBus.Dispatcher.WaitForResult(requestId, 2500, out result))
            {
                HttpResponseWriter.WriteApiError(context.Response, 503, "MOD_NOT_READY", "Main-thread processor not ready");
                return;
            }

            if (result == null)
            {
                HttpResponseWriter.WriteApiError(context.Response, 503, "MOD_NOT_READY", "No command result produced");
                return;
            }

            if (!result.Success)
            {
                var status = ErrorToStatus(result.Error);
                HttpResponseWriter.WriteApiError(context.Response, status, result.Error ?? "EVENT_FAILED", "Command failed");
                return;
            }

            var json = result.Data as string;
            if (string.IsNullOrEmpty(json))
            {
                HttpResponseWriter.WriteApiError(context.Response, 503, "MOD_NOT_READY", "Invalid command payload");
                return;
            }

            HttpResponseWriter.WriteJson(context.Response, 200, json);
        }

        private int ErrorToStatus(string error)
        {
            if (string.IsNullOrEmpty(error))
            {
                return 409;
            }

            switch (error)
            {
                case "UNAUTHORIZED":
                    return 401;
                case "INVALID_REQUEST":
                case "INVALID_EVENT":
                    return 400;
                case "NO_COLONY_LOADED":
                case "EVENT_FAILED":
                case "EVENT_BLOCKED":
                    return 409;
                case "RATE_LIMITED":
                case "COOLDOWN_ACTIVE":
                    return 429;
                case "GAME_NOT_RUNNING":
                case "MOD_NOT_READY":
                    return 503;
                default:
                    return 409;
            }
        }

        private long UptimeSeconds()
        {
            var uptime = (long)(DateTime.UtcNow - _startedAtUtc).TotalSeconds;
            return uptime < 0 ? 0 : uptime;
        }
    }
}
