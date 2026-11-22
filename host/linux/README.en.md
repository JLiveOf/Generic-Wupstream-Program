# Generic Wupstream Program for Linux

[Guía en español](README.md)

Install the x86-64 package with `sudo apt install ./generic-wupstream_2.2.1+experimental.1_amd64.deb`. Open **Generic Wupstream Program** from the launcher or run `generic-wupstream`.

In the GamePad's Homebrew Menu, install and open the official **WUP Stream** from the App Store. Enter the IP shown on the GamePad and choose a frame rate and quality. The app streams H.264 at 854 × 480 with capture rates from 15 to 60 FPS. Audio and virtual controller support are pending on Linux.

On X11, the app captures the X11 desktop. On Wayland, choose a display from the system picker. Wayland requires PipeWire and a ScreenCast portal that supports your compositor, such as `xdg-desktop-portal-gnome`, `xdg-desktop-portal-kde`, or `xdg-desktop-portal-wlr` on wlroots desktops.

The **Reportar un problema en GitHub** button opens a GitHub issue form with a short diagnostic log. Review it before clicking **Submit new issue**.
