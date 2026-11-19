#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
set -eu
cd -- "$(dirname -- "$0")/.."

revision=b35605ace3ddf7c1a5d67a2eb553f034aef41d55
patch="$PWD/x264-drh-mode.patch"
x264="${WUPSTREAM_X264_DIR:-$PWD/../build/host/x264-windows-simd}"
if [ ! -f "$x264/configure" ]; then
    mkdir -p "$x264"
    git -C "$x264" init -q
    git -C "$x264" fetch -q --depth 1 https://github.com/mirror/x264.git "$revision"
    git -C "$x264" checkout -q FETCH_HEAD
fi
test "$(git -C "$x264" rev-parse HEAD)" = "$revision"
if ! git -C "$x264" apply --reverse --check "$patch" 2>/dev/null; then
    git -C "$x264" apply "$patch"
fi

if [ ! -f "$x264/libx264.a" ]; then
    if ! command -v nasm >/dev/null; then
        printf '%s\n' 'NASM is required for real-time encoding. Add it to PATH and rebuild.' >&2
        exit 1
    fi
    (
        cd "$x264"
        bash ./configure --host=x86_64-w64-mingw32 --enable-static --disable-cli \
            --disable-opencl --bit-depth=8
        make -j8 libx264.a
    )
fi

if ! grep -qx '#define HAVE_MMX 1' "$x264/config.h"; then
    printf '%s\n' 'The cached x264 was built without SIMD. Use a fresh WUPSTREAM_X264_DIR.' >&2
    exit 1
fi

gcc -shared -static -O2 -Wall -Wextra -I"$x264" wupstream.c \
    windows/StreamBridge.c windows/WindowsAudio.c "$x264/libx264.a" \
    -lwinpthread -lws2_32 -lwinmm -lole32 -luuid -o "$WUPSTREAM_NATIVE_DEST"
