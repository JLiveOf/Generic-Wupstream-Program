#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-or-later
"""Make ordinary launches of the pinned amd64 AppImage runtime extract and run."""
import argparse
import hashlib
from pathlib import Path

RUNTIME_SIZE = 944632
RUNTIME_HASH = "617aa77314d57c8b8e4fae9c3f0b35af7b30d5529cb69e98f64f92510ed98a46"
MD5_FIELD = slice(0xE3900, 0xE3910)
DISPATCH = slice(0x2DAD6, 0x2DADF)


def build(source, output):
    appimage = source.read_bytes()
    runtime = bytearray(appimage[:RUNTIME_SIZE])
    runtime[MD5_FIELD] = bytes(16)
    if (hashlib.sha256(runtime).hexdigest() != RUNTIME_HASH
            or appimage[RUNTIME_SIZE:RUNTIME_SIZE + 4] != b"hsqs"):
        raise ValueError("Expected the pinned unsigned amd64 type-2 runtime and SquashFS payload")

    # At this point r12 holds the parsed AppImage option (NULL for an ordinary
    # launch). Branch to the upstream extract-and-run code when it is NULL.
    # Explicit runtime options, including --appimage-extract-and-run, retain dispatch.
    if runtime[DISPATCH] != bytes.fromhex("48 85 c0 0f 85 53 04 00 00"):
        raise ValueError("Unexpected runtime dispatch")
    runtime[DISPATCH] = bytes.fromhex("4d 85 e4 0f 84 53 04 00 00")
    output.write_bytes(runtime + appimage[RUNTIME_SIZE:])
    output.chmod(0o755)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    build(args.source, args.output)
