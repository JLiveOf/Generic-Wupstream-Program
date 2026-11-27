#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-or-later
import argparse
import io
import tarfile
from pathlib import Path, PurePosixPath

HERE = Path(__file__).resolve().parent


def tar_gz(files):
    output = io.BytesIO()
    with tarfile.open(fileobj=output, mode="w:gz", format=tarfile.USTAR_FORMAT) as archive:
        directories = set()
        for name, data, mode in files:
            for parent in reversed(PurePosixPath(name).parents):
                if parent == PurePosixPath(".") or parent in directories:
                    continue
                directory = tarfile.TarInfo("./" + parent.as_posix())
                directory.type = tarfile.DIRTYPE
                directory.mode = 0o755
                archive.addfile(directory)
                directories.add(parent)
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
        ("usr/bin/generic-wupstream", (HERE / "generic-wupstream").read_bytes().replace(b"\r\n", b"\n"), 0o755),
        ("usr/lib/generic-wupstream/wup_gamepad.py", (HERE / "wup_gamepad.py").read_bytes(), 0o644),
        ("usr/lib/generic-wupstream/wup_ui.py", (HERE / "wup_ui.py").read_bytes(), 0o644),
        ("usr/lib/udev/rules.d/70-generic-wupstream-uinput.rules",
         (HERE / "70-generic-wupstream-uinput.rules").read_bytes(), 0o644),
        ("usr/lib/modules-load.d/generic-wupstream.conf", (HERE / "generic-wupstream.conf").read_bytes(), 0o644),
        ("usr/share/applications/generic-wupstream.desktop", (HERE / "generic-wupstream.desktop").read_bytes(), 0o644),
        ("usr/share/icons/hicolor/256x256/apps/generic-wupstream.png", (HERE.parent / "assets/app.png").read_bytes(), 0o644),
        ("usr/share/doc/generic-wupstream/README.md", (HERE / "README.md").read_bytes(), 0o644),
        ("usr/share/doc/generic-wupstream/README.en.md", (HERE / "README.en.md").read_bytes(), 0o644),
        ("usr/share/doc/generic-wupstream/copyright", (HERE.parent.parent / "LICENSE").read_bytes(), 0o644),
        ("usr/lib/generic-wupstream/libwupstream.so", binary, 0o644),
    ]
    installed = sum(len(content) for _, content, _ in data)
    control = (
        "Package: generic-wupstream\nVersion: 2.2.4+experimental.1\nArchitecture: amd64\nSection: video\n"
        "Priority: optional\nMaintainer: Generic Wupstream Program contributors\n"
        f"Installed-Size: {(installed + 1023) // 1024}\n"
        "Depends: libc6 (>= 2.28), python3, python3-gi, python3-gi-cairo, python3-evdev, gir1.2-gtk-3.0, gir1.2-gstreamer-1.0, "
        "gstreamer1.0-plugins-base, gstreamer1.0-plugins-good, gstreamer1.0-x, "
        "gstreamer1.0-pipewire, xdg-desktop-portal\n"
        "Description: Use a Wii U GamePad as a Linux game controller and display\n"
        " Captures the desktop on X11 or via the ScreenCast portal on Wayland.\n"
        " Exposes a virtual controller via uinput and uses the official WUP Stream receiver.\n"
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
