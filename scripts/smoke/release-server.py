#!/usr/bin/env python3
"""Serve installer smoke fixtures with GitHub-like JSON content types."""

import functools
import http.server
import pathlib
import sys


class ReleaseFixtureHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path: str) -> str:
        fixture_path = pathlib.PurePosixPath(path)
        if fixture_path.name == "latest" or fixture_path.parent.name == "tags":
            return "application/json"
        return super().guess_type(path)


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit("usage: release-server.py <directory> <port>")

    directory, port_text = sys.argv[1:]
    handler = functools.partial(ReleaseFixtureHandler, directory=directory)
    server = http.server.ThreadingHTTPServer(("127.0.0.1", int(port_text)), handler)
    server.serve_forever()


if __name__ == "__main__":
    main()
