# Generic Wupstream Program para Linux

Instala el paquete x86-64 con `sudo apt install ./generic-wupstream_2.2.1+experimental.1_amd64.deb`.
Abre **Generic Wupstream Program** desde el lanzador o ejecuta `generic-wupstream`.

En el Homebrew Menu del GamePad, instala **WUP Stream** desde la App Store y
ábrelo. Introduce la IP que aparece en el GamePad y elige FPS y calidad.
El paquete usa H.264 a 854 × 480 y ofrece de 15 a 60 FPS de captura.
El audio y el mando virtual quedan pendientes en Linux.

En X11 se captura el escritorio X11. En Wayland, elige un monitor en el selector
del sistema. La sesión Wayland necesita PipeWire y un portal ScreenCast
compatible con el compositor, como `xdg-desktop-portal-gnome`,
`xdg-desktop-portal-kde` o `xdg-desktop-portal-wlr` en escritorios wlroots.

**Reportar un problema en GitHub** abre el formulario con un diagnóstico breve. Revisa los datos y pulsa **Submit new issue** en GitHub para publicarlos.
