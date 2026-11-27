#!/usr/bin/env bash
set -euo pipefail

# Run in the Debian 11 amd64 build container used by the release workflow.
printf '%s\n' 'deb http://deb.debian.org/debian bullseye main' > /tmp/wup-debian.list
apt_args=(-o Dir::Etc::sourcelist=/tmp/wup-debian.list -o Dir::Etc::sourceparts=-)
apt-get "${apt_args[@]}" update -qq
apt-get "${apt_args[@]}" install -qy --no-install-recommends \
  ca-certificates curl file python3-gi python3-gi-cairo python3-evdev gir1.2-gtk-3.0 \
  gir1.2-gstreamer-1.0 gstreamer1.0-plugins-base gstreamer1.0-plugins-good \
  gstreamer1.0-x gstreamer1.0-pipewire xvfb xauth xdotool squashfs-tools \
  desktop-file-utils adwaita-icon-theme fonts-dejavu-core librsvg2-common shared-mime-info

temp=$(mktemp -d)
trap 'rm -rf -- "$temp"' EXIT
curl -fsSL --retry 3 https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage -o "$temp/appimagetool"
curl -fsSL --retry 3 https://github.com/AppImage/type2-runtime/releases/download/continuous/runtime-x86_64 -o "$temp/runtime"
chmod +x "$temp/appimagetool"
APPIMAGE_RUNTIME_FILE="$temp/runtime" python3 host/linux/build-appimage.py \
  build/linux/generic-wupstream_2.2.4+experimental.1_amd64.deb "$temp/appimagetool" "$temp/raw.AppImage"
appimage=build/linux/Generic-Wupstream-Program-v2.2.4-x86_64.AppImage
python3 host/linux/appimage-extract-default.py "$temp/raw.AppImage" "$appimage"
env -u APPIMAGE_EXTRACT_AND_RUN GDK_BACKEND=x11 xvfb-run -a bash -e -c '
  "$1" > "$2" 2>&1 &
  pid=$!
  trap "kill $pid 2>/dev/null || true" EXIT
  timeout 20 xdotool search --sync --onlyvisible --name "^Generic Wupstream Program$" || { cat "$2"; exit 1; }
  kill -0 "$pid"
' bash "$appimage" "$temp/launch.log"
