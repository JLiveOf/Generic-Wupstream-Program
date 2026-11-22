# Generic Wupstream Program para Linux

[English guide](README.en.md)

Instala el paquete x86-64 con `sudo apt install ./generic-wupstream_2.2.2+experimental.1_amd64.deb`.
Reinicia Linux tras la instalación para activar el módulo y la regla de acceso a `/dev/uinput` para la sesión local activa.
Abre **Generic Wupstream Program** desde el lanzador o ejecuta `generic-wupstream`.

En el Homebrew Menu del GamePad, instala **WUP Stream** desde la App Store y
ábrelo. Introduce la IP que aparece en el GamePad y elige FPS y calidad.
El paquete usa H.264 a 854 × 480 y ofrece de 15 a 60 FPS de captura.
El audio queda pendiente en Linux.

Al conectar aparece **Generic Wupstream Gamepad**, un mando virtual Linux con botones, cruceta, gatillos y dos sticks. En Steam, abre **Configuración → Mando** para comprobar los botones. Activa **ABXY por posición Xbox** para que las posiciones coincidan con un mando Xbox, o desactívalo para seguir las letras del GamePad.

Desmarca **Enviar pantalla** para usar solo los controles. En este modo no se abre el selector de captura de Wayland.

En X11 se captura el escritorio X11. En Wayland, elige un monitor en el selector
del sistema. La sesión Wayland necesita PipeWire y un portal ScreenCast
compatible con el compositor, como `xdg-desktop-portal-gnome`,
`xdg-desktop-portal-kde` o `xdg-desktop-portal-wlr` en escritorios wlroots.

**Reportar un problema en GitHub** abre el formulario con un diagnóstico breve. Revisa los datos y pulsa **Submit new issue** en GitHub para publicarlos.
