#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-or-later
import argparse
import io
import tarfile
from pathlib import Path

HERE = Path(__file__).resolve().parent


def tar_gz(files):
    output = io.BytesIO()
    with tarfile.open(fileobj=output, mode="w:gz", format=tarfile.USTAR_FORMAT) as archive:
        for name, data, mode in files:
            entry = tarfile.TarInfo("./" + name)
            entry.size = len(data)
            entry.mode = mode
            archive.addfile(entry, io.BytesIO(data))
    return output.getvalue()


def ar_member(name, data):
    header = f"{name + '/':<16}{0:<12}{0:<6}{0:<6}{'100644':<8}{len(data):<10}`\n".encode("ascii")
    return header + data + (b"\n" if len(data) % 2 else b"")


def build(output, engine):
    binary = engine.read_bytes()
    if binary[:6] != b"\x7fELF\x02\x01" or binary[18:20] != b"\x3e\x00":
        raise ValueError("Expected an amd64 Linux ELF library")
    data = [
        ("usr/bin/generic-wupstream", (HERE / "generic-wupstream").read_bytes(), 0o755),
        ("usr/share/applications/generic-wupstream.desktop", (HERE / "generic-wupstream.desktop").read_bytes(), 0o644),
        ("usr/share/icons/hicolor/256x256/apps/generic-wupstream.png", (HERE.parent / "assets/app.png").read_bytes(), 0o644),
        ("usr/share/doc/generic-wupstream/README.md", (HERE / "README.md").read_bytes(), 0o644),
        ("usr/share/doc/generic-wupstream/README.en.md", (HERE / "README.en.md").read_bytes(), 0o644),
        ("usr/share/doc/generic-wupstream/copyright", (HERE.parent.parent / "LICENSE").read_bytes(), 0o644),
        ("usr/lib/generic-wupstream/libwupstream.so", binary, 0o644),
    ]
    installed = sum(len(content) for _, content, _ in data)
    control = (
        "Package: generic-wupstream\nVersion: 2.2.1+experimental.1\nArchitecture: amd64\nSection: video\n"
        "Priority: optional\nMaintainer: Generic Wupstream Program contributors\n"
        f"Installed-Size: {(installed + 1023) // 1024}\n"
        "Depends: libc6 (>= 2.28), python3, python3-gi, gir1.2-gtk-3.0, gir1.2-gstreamer-1.0, "
        "gstreamer1.0-plugins-base, gstreamer1.0-plugins-good, gstreamer1.0-x, "
        "gstreamer1.0-pipewire, xdg-desktop-portal\n"
        "Description: Stream a Linux desktop to a Wii U GamePad\n"
        " Captures the desktop on X11 or via the ScreenCast portal on Wayland.\n"
        " Uses the official WUP Stream receiver on the GamePad.\n"
    ).encode()
    output.write_bytes(b"!<arch>\n" + ar_member("debian-binary", b"2.0\n") +
                       ar_member("control.tar.gz", tar_gz([("control", control, 0o644)])) +
                       ar_member("data.tar.gz", tar_gz(data)))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    parser.add_argument("engine", type=Path)
    arguments = parser.parse_args()
    build(arguments.output, arguments.engine)
