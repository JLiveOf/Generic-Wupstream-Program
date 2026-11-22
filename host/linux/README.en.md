# Generic Wupstream Program for Linux

[Guía en español](README.md)

Install the x86-64 package with `sudo apt install ./generic-wupstream_2.2.2+experimental.1_amd64.deb`. Reboot to load the module and apply the `/dev/uinput` access rule for the active local session. Open **Generic Wupstream Program** from the launcher or run `generic-wupstream`.

In the GamePad's Homebrew Menu, install and open the official **WUP Stream** from the App Store. Enter the IP shown on the GamePad and choose a frame rate and quality. The app streams H.264 at 854 × 480 with capture rates from 15 to 60 FPS. Audio support is pending on Linux.

When you connect, **Generic Wupstream Gamepad** appears as a Linux virtual controller with buttons, D-pad, triggers and both sticks. Test it in **Steam → Settings → Controller**. Enable **ABXY por posición Xbox** to match Xbox button positions or clear it to match the printed GamePad labels.

Clear **Enviar pantalla** to play with controller input only. This mode skips the Wayland screen picker.

On X11, the app captures the X11 desktop. On Wayland, choose a display from the system picker. Wayland requires PipeWire and a ScreenCast portal that supports your compositor, such as `xdg-desktop-portal-gnome`, `xdg-desktop-portal-kde`, or `xdg-desktop-portal-wlr` on wlroots desktops.

The **Reportar un problema en GitHub** button opens a GitHub issue form with a short diagnostic log. Review it before clicking **Submit new issue**.
