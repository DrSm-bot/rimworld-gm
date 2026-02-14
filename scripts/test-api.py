#!/usr/bin/env python3
"""Contract tests for Rimworld GM API.

Usage:
  python scripts/test-api.py --mock
  python scripts/test-api.py --base-url http://localhost:18800
"""

from __future__ import annotations

import argparse
import json
import sys
import threading
import time
from dataclasses import dataclass
from http.server import BaseHTTPRequestHandler, HTTPServer
from urllib import error, parse, request


@dataclass
class TestResult:
    name: str
    ok: bool
    detail: str = ""


def http_call(method: str, url: str, body: dict | None = None) -> tuple[int, dict]:
    data = None
    headers = {}
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"

    req = request.Request(url=url, data=data, method=method, headers=headers)
    try:
        with request.urlopen(req, timeout=3) as resp:
            payload = json.loads(resp.read().decode("utf-8") or "{}")
            return resp.status, payload
    except error.HTTPError as e:
        payload = {}
        try:
            payload = json.loads(e.read().decode("utf-8") or "{}")
        except Exception:
            pass
        return e.code, payload


class MockHandler(BaseHTTPRequestHandler):
    server_version = "RimworldGMMock/0.1"

    def _json(self, status: int, payload: dict):
        out = json.dumps(payload).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(out)))
        self.end_headers()
        self.wfile.write(out)

    def log_message(self, *_):
        return

    def do_GET(self):
        parsed = parse.urlparse(self.path)
        q = parse.parse_qs(parsed.query)

        if parsed.path == "/health":
            if q.get("error") == ["not_ready"]:
                return self._json(503, {"success": False, "error": "MOD_NOT_READY", "message": "mock not ready"})
            return self._json(200, {
                "status": "ok", "game_running": True, "colony_loaded": True,
                "mod_version": "0.1.0", "queue_depth": 0, "uptime_seconds": 12
            })

        if parsed.path == "/state":
            if q.get("error") == ["no_colony"]:
                return self._json(409, {"success": False, "error": "NO_COLONY_LOADED", "message": "no colony"})
            return self._json(200, {
                "colony": {"name": "Mock", "wealth": 1, "day": 1, "season": "Spring", "quadrum": "Aprimay"},
                "colonists": [],
                "resources": {"silver": 0, "food": 0, "medicine": 0, "components": 0},
                "threats": {"active_raids": 0, "nearby_enemies": False, "toxic_fallout": False}
            })

        return self._json(404, {"success": False, "error": "NOT_FOUND", "message": "mock route not found"})

    def do_POST(self):
        parsed = parse.urlparse(self.path)
        content_len = int(self.headers.get("Content-Length", "0"))
        body = {}
        if content_len > 0:
            body = json.loads(self.rfile.read(content_len).decode("utf-8"))

        if parsed.path == "/event":
            event_type = body.get("event_type")
            if not event_type:
                return self._json(400, {"success": False, "error": "INVALID_REQUEST", "message": "event_type required"})
            if event_type == "invalid_event":
                return self._json(400, {"success": False, "error": "INVALID_EVENT", "message": "invalid"})
            return self._json(200, {"success": True, "message": "ok", "event_id": "evt_mock"})

        if parsed.path == "/message":
            text = body.get("text", "")
            if not text:
                return self._json(400, {"success": False, "error": "INVALID_REQUEST", "message": "text required"})
            return self._json(200, {"success": True})

        return self._json(404, {"success": False, "error": "NOT_FOUND", "message": "mock route not found"})


def start_mock_server(port: int = 18899):
    httpd = HTTPServer(("127.0.0.1", port), MockHandler)
    t = threading.Thread(target=httpd.serve_forever, daemon=True)
    t.start()
    time.sleep(0.1)
    return httpd, f"http://127.0.0.1:{port}"


def run_tests(base_url: str) -> list[TestResult]:
    tests: list[TestResult] = []

    def check(name: str, condition: bool, detail: str = ""):
        tests.append(TestResult(name=name, ok=condition, detail=detail))

    status, body = http_call("GET", f"{base_url}/health")
    check("health happy", status == 200 and body.get("status") in {"ok", "degraded"}, f"status={status}, body={body}")

    status, body = http_call("GET", f"{base_url}/state")
    check("state happy", status == 200 and "colony" in body and "threats" in body, f"status={status}, body={body}")

    status, body = http_call("POST", f"{base_url}/event", {"event_type": "raid", "params": {"points": 500}})
    check("event happy", status == 200 and body.get("success") is True, f"status={status}, body={body}")

    status, body = http_call("POST", f"{base_url}/event", {"event_type": "invalid_event"})
    check("event error path", status == 400 and body.get("error") in {"INVALID_EVENT", "INVALID_REQUEST"}, f"status={status}, body={body}")

    status, body = http_call("POST", f"{base_url}/message", {"text": "Hello colony", "type": "info", "duration": 5})
    check("message happy", status == 200 and body.get("success") is True, f"status={status}, body={body}")

    status, body = http_call("POST", f"{base_url}/message", {"type": "info"})
    check("message error path", status == 400 and body.get("error") == "INVALID_REQUEST", f"status={status}, body={body}")

    return tests


def print_results(results: list[TestResult]) -> int:
    failed = [r for r in results if not r.ok]
    for r in results:
        prefix = "✅" if r.ok else "❌"
        print(f"{prefix} {r.name}")
        if not r.ok:
            print(f"   {r.detail}")
    print()
    print(f"Summary: {len(results) - len(failed)}/{len(results)} passed")
    return 1 if failed else 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Rimworld GM API contract tests")
    parser.add_argument("--base-url", default="http://localhost:18800", help="Target base URL")
    parser.add_argument("--mock", action="store_true", help="Run against local mock server")
    args = parser.parse_args()

    mock = None
    base_url = args.base_url
    try:
        if args.mock:
            mock, base_url = start_mock_server()
            print(f"Running against mock server at {base_url}")
        else:
            print(f"Running against target {base_url}")

        results = run_tests(base_url)
        return print_results(results)
    finally:
        if mock is not None:
            mock.shutdown()


if __name__ == "__main__":
    sys.exit(main())