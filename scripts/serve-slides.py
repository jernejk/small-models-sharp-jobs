#!/usr/bin/env python3
"""Local deck server for the workshop. No-store so an edit shows on refresh."""
import functools, http.server, os, socketserver

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "facilitator", "workshop")
PORT = 5173


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Cache-Control", "no-store, must-revalidate")
        super().end_headers()

    def send_header(self, key, value):
        if key.lower() in ("last-modified", "etag"):
            return
        super().send_header(key, value)


socketserver.TCPServer.allow_reuse_address = True
with socketserver.TCPServer(("127.0.0.1", PORT), functools.partial(Handler, directory=os.path.realpath(ROOT))) as httpd:
    print(f"slides on http://localhost:{PORT}/slides/", flush=True)
    httpd.serve_forever()
