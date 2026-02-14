using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using RimworldGM.Core;
using Verse;

namespace RimworldGM.Http
{
    /// <summary>
    /// Local HTTP server for Rimworld GM integration.
    /// PR #3 scope: route /health through command queue/main-thread pump.
    /// </summary>
    public class HttpServer
    {
        private readonly int _port;
        private HttpListener _listener;
        private Thread _listenerThread;
        private volatile bool _running;
        private DateTime _startedAtUtc;

        public HttpServer(int port)
        {
            _port = port;
        }

        public void Start()
        {
            if (_running)
            {
                return;
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:" + _port + "/");
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
            catch (Exception)
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
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            if (RequestParser.IsHealthRequest(context.Request))
            {
                var uptimeSeconds = (long)(DateTime.UtcNow - _startedAtUtc).TotalSeconds;
                if (uptimeSeconds < 0)
                {
                    uptimeSeconds = 0;
                }

                var args = new Dictionary<string, string>();
                args["uptime_seconds"] = uptimeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

                var command = new GameCommand(GameCommandKind.Health, args);
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
                    HttpResponseWriter.WriteApiError(context.Response, 503, result.Error ?? "MOD_NOT_READY", "Health command failed");
                    return;
                }

                var json = result.Data as string;
                if (string.IsNullOrEmpty(json))
                {
                    HttpResponseWriter.WriteApiError(context.Response, 503, "MOD_NOT_READY", "Invalid health payload");
                    return;
                }

                HttpResponseWriter.WriteJson(context.Response, 200, json);
                return;
            }

            HttpResponseWriter.WriteNotFound(context.Response);
        }
    }
}
