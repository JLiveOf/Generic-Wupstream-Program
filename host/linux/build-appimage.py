#!/usr/bin/python3
# SPDX-License-Identifier: GPL-3.0-or-later
"""Bundle the tested Debian 11 amd64 desktop runtime into an AppImage."""
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
LIB = Path("/usr/lib/x86_64-linux-gnu")
PLUGINS = LIB / "gstreamer-1.0"
SYSTEM_LIBS = re.compile(r"^(?:ld-linux|lib(?:c|m|pthread|dl|rt|resolv|util|anl|nsl|GL|EGL|drm|gbm)\.so)")
VIDEO_PLUGINS = ("coreelements", "app", "videoconvert", "videorate", "videoscale", "ximagesrc")


def bundle(package, appimagetool, output):
    appdir = output.parent / "GenericWupstream.AppDir"
    appdir.mkdir()
    subprocess.run(["dpkg-deb", "-x", str(package), str(appdir)], check=True)

    def copy(source):
        target = appdir / source.relative_to("/")
        if target.exists():
            return
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)

    def tree(source):
        for item in source.rglob("*"):
            if item.is_file() and "__pycache__" not in item.parts:
                copy(item)

    copy(Path("/usr/bin/python3.9"))
    for directory in (Path("/usr/lib/python3.9"), Path("/usr/lib/python3/dist-packages/gi"),
                      Path("/usr/lib/python3/dist-packages/evdev"), Path("/usr/lib/python3/dist-packages/cairo"),
                      LIB / "girepository-1.0", Path("/usr/share/glib-2.0/schemas"),
                      Path("/usr/share/icons/Adwaita"), Path("/usr/share/icons/hicolor"),
                      Path("/usr/share/mime")):
        tree(directory)
    for video in VIDEO_PLUGINS:
        copy(PLUGINS / f"libgst{video}.so")
    copy(LIB / "gstreamer1.0/gstreamer-1.0/gst-plugin-scanner")
    pixbuf = LIB / "gdk-pixbuf-2.0"
    loaders = pixbuf / "2.10.0/loaders"
    image_loaders = [loaders / f"libpixbufloader-{format_name}.so" for format_name in ("png", "svg")]
    for loader in image_loaders:
        copy(loader)
    query = subprocess.check_output(
        [str(pixbuf / "gdk-pixbuf-query-loaders"), *map(str, image_loaders)], text=True
    )
    (appdir / "pixbuf-loaders.cache.in").write_text(
        query.replace(f'"{loaders}/', f'"@APPDIR@{loaders}/')
    )

    # Typelibs load GTK/GStreamer libraries with dlopen, which ldd cannot infer
    # from the Python executable alone.
    for name in ("libgtk-3.so.0", "libgdk-3.so.0", "libgstvideo-1.0.so.0",
                 "libgstreamer-1.0.so.0", "libgdk_pixbuf-2.0.so.0", "libXtst.so.6"):
        copy(LIB / name)
    inspected = set()
    while True:
        pending = [file for file in appdir.rglob("*") if file.is_file() and file not in inspected]
        if not pending:
            break
        for binary in pending:
            inspected.add(binary)
            with binary.open("rb") as handle:
                if handle.read(4) != b"\x7fELF":
                    continue
            trace = subprocess.run(["ldd", str(binary)], check=True, text=True, capture_output=True).stdout
            for lib, path in re.findall(r"(\S+)\s+=>\s+(/\S+)\s+", trace):
                if not SYSTEM_LIBS.match(lib):
                    copy(Path(path))

    launcher = appdir / "AppRun"
    launcher.write_bytes((HERE / "AppRun").read_bytes().replace(b"\r\n", b"\n"))
    launcher.chmod(0o755)
    shutil.copy2(appdir / "usr/share/applications/generic-wupstream.desktop", appdir)
    shutil.copy2(appdir / "usr/share/icons/hicolor/256x256/apps/generic-wupstream.png", appdir)
    env = {**os.environ, "ARCH": "x86_64", "APPIMAGE_EXTRACT_AND_RUN": "1"}
    runtime = os.environ.get("APPIMAGE_RUNTIME_FILE")
    options = ["--runtime-file", runtime] if runtime else []
    subprocess.run([str(appimagetool), *options, str(appdir), str(output)], env=env, check=True)


if __name__ == "__main__":
    bundle(*(Path(value).resolve() for value in sys.argv[1:]))
