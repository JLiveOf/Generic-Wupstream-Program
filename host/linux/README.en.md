# Generic Wupstream Program for Linux

[Guía en español](README.md)

## Requirements

| Use | Requirements |
| --- | --- |
| Connect the GamePad | An x86-64 Linux PC and the GamePad on the same network. Open the official **WUP Stream** receiver from the Homebrew Menu. |
| `.deb` package | Debian, Ubuntu, Mint, or another APT-based distribution. `apt install ./file.deb` installs available dependencies from the system's repositories, including Python 3, GTK 3, evdev, and GStreamer. |
| AppImage | An x86-64 Linux graphical session with glibc 2.31 or newer. It bundles Python, GTK, evdev, and X11 capture. Wayland uses the system's `gst-launch-1.0` and `pipewiresrc` plugin. It extracts itself automatically without FUSE. |
| X11 capture | An X11 graphical session. |
| Wayland capture | PipeWire and `xdg-desktop-portal` with a ScreenCast backend for your compositor. The system displays the screen picker. |
| PC audio and virtual microphone | A PulseAudio-compatible server, including PipeWire with `pipewire-pulse`, and the `pactl` and `parec` commands. |
| Virtual controller | The kernel's `uinput` module and write access to `/dev/uinput` for your user session. The `.deb` installs the system configuration; see below for AppImage-only setup. |

## Install the `.deb`

Install the x86-64 package with `sudo apt install ./generic-wupstream_2.2.5+experimental.1_amd64.deb`. Reboot to load the module and apply the `/dev/uinput` access rule for the active local session. Open **Generic Wupstream Program** from the launcher or run `generic-wupstream`.

In the GamePad's Homebrew Menu, install and open the official **WUP Stream** from the App Store. Enter the IP shown on the GamePad and choose a frame rate and quality. The app streams H.264 at 854 × 480 with capture rates from 15 to 60 FPS. The selected Wayland frame rate is a ceiling.

Enable **Send PC audio** to listen to the default Linux output on the GamePad.
**Use microphone** enables the level meter and lets you record a mono, 48 kHz WAV.
With **Create virtual microphone** enabled, choose **Wupstream
GamePad Microphone** as the input in your audio application.

When you connect, **Generic Wupstream Gamepad** appears as a Linux virtual controller with buttons, D-pad, triggers and both sticks. Test it in **Steam → Settings → Controller**. Enable **ABXY por posición Xbox** to match Xbox button positions or clear it to match the printed GamePad labels.

Clear **Send screen** to show a **Game Controller Mode** image without sharing the desktop. Controller input, audio, and microphone remain available in this mode.

Use **Español / English** in the top-right corner to switch languages. **Live controls** shows buttons, sticks, and battery status. Leave the IP field blank to discover the GamePad if its UDP announcements reach the PC.

On X11, choose the entire desktop or one monitor. On Wayland, choose a display from the system picker. Wayland requires PipeWire and a ScreenCast portal that supports your compositor, such as `xdg-desktop-portal-gnome`, `xdg-desktop-portal-kde`, or `xdg-desktop-portal-wlr` on wlroots desktops. **Touchscreen as mouse** also requires a Wayland RemoteDesktop backend with pointer support. Enable remote interaction when the picker offers it.

The **Reportar un problema en GitHub** button opens a GitHub issue form with a short diagnostic log. Review it before clicking **Submit new issue**.

## AppImage

On an x86-64 Linux system with glibc 2.31 or later, make the AppImage executable
with `chmod +x Generic-Wupstream-Program-v2.2.5-x86_64.AppImage` and run it.
### What is FUSE?

FUSE (*Filesystem in Userspace*) lets a Linux program mount a filesystem.
A conventional AppImage uses it to read its compressed contents directly.
The 2.2.5 AppImage automatically extracts itself into a temporary directory
when opened. After marking the file executable, you can launch it with a
double-click without installing FUSE or passing command-line options.
[Official AppImage documentation on FUSE](https://docs.appimage.org/user-guide/troubleshooting/fuse.html).

### Enable the virtual controller with the AppImage

If you already installed the `.deb` and rebooted, its rule is set up. If you
only use the AppImage, download
[`70-generic-wupstream-uinput.rules`](70-generic-wupstream-uinput.rules)
and run the following from the directory where you saved it:

```sh
sudo install -m 644 70-generic-wupstream-uinput.rules /etc/udev/rules.d/
printf 'uinput\n' | sudo tee /etc/modules-load.d/generic-wupstream.conf
```

Reboot and run the app as your normal user. Check access with
`test -w /dev/uinput && echo "uinput available"`. Screen streaming remains
available when the virtual controller cannot be created.
