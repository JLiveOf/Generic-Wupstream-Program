# Generic Wupstream Program para Linux

[English guide](README.en.md)

## Requisitos

| Uso | Requisitos |
| --- | --- |
| Conectar el GamePad | PC Linux x86-64 y GamePad en la misma red. Abre el receptor oficial **WUP Stream** en el Homebrew Menu. |
| Paquete `.deb` | Debian, Ubuntu, Mint o compatible con APT. `apt install ./archivo.deb` instala las dependencias disponibles en los repositorios del sistema, entre ellas Python 3, GTK 3, evdev y GStreamer. |
| AppImage | Linux x86-64 con glibc 2.31 o posterior y una sesión gráfica. Incluye Python, GTK, evdev y los componentes de GStreamer que utiliza. La variante **sin FUSE** se extrae automáticamente al abrirla. |
| Captura en X11 | Una sesión gráfica X11. |
| Captura en Wayland | PipeWire y `xdg-desktop-portal` con un backend que implemente ScreenCast para tu compositor. El selector de pantalla lo muestra el sistema. |
| Mando virtual | Módulo del kernel `uinput` y permiso de escritura a `/dev/uinput` en la sesión del usuario. El `.deb` instala la configuración necesaria. Si usas solo el AppImage, consulta los pasos de abajo. |

## Instalación con el `.deb`

Instala el paquete x86-64 con `sudo apt install ./generic-wupstream_2.2.3+experimental.1_amd64.deb`.
Reinicia Linux tras la instalación para activar el módulo y la regla de acceso a `/dev/uinput` para la sesión local activa.
Abre **Generic Wupstream Program** desde el lanzador o ejecuta `generic-wupstream`.

En el Homebrew Menu del GamePad, instala **WUP Stream** desde la App Store y
ábrelo. Introduce la IP que aparece en el GamePad y elige FPS y calidad.
El paquete usa H.264 a 854 × 480 y ofrece de 15 a 60 FPS de captura.
El audio queda pendiente en Linux.

Al conectar aparece **Generic Wupstream Gamepad**, un mando virtual Linux con botones, cruceta, gatillos y dos sticks. En Steam, abre **Configuración → Mando** para comprobar los botones. Activa **ABXY por posición Xbox** para que las posiciones coincidan con un mando Xbox, o desactívalo para seguir las letras del GamePad.

Desmarca **Enviar pantalla** para usar solo los controles. En este modo no se abre el selector de captura de Wayland.

Usa **Español / English** en la esquina superior derecha para cambiar el idioma. El panel **Controles en directo** muestra los botones, sticks y batería. Puedes dejar la IP vacía si el anuncio UDP del GamePad llega al PC.

En X11 se captura el escritorio X11. En Wayland, elige un monitor en el selector
del sistema. La sesión Wayland necesita PipeWire y un portal ScreenCast
compatible con el compositor, como `xdg-desktop-portal-gnome`,
`xdg-desktop-portal-kde` o `xdg-desktop-portal-wlr` en escritorios wlroots.

**Reportar un problema en GitHub** abre el formulario con un diagnóstico breve. Revisa los datos y pulsa **Submit new issue** en GitHub para publicarlos.

## AppImage

Para probar el AppImage en un Linux x86-64 con glibc 2.31 o posterior:

```sh
chmod +x Generic-Wupstream-Program-v2.2.3-x86_64.AppImage
./Generic-Wupstream-Program-v2.2.3-x86_64.AppImage
```

Si el AppImage normal falla con FUSE, usa la variante **sin FUSE** o añade
`--appimage-extract-and-run` al comando.

### Activar el mando virtual con el AppImage

Si ya instalaste el `.deb` y reiniciaste, el sistema ya tiene su regla. Si usas
solo el AppImage, descarga la
[`70-generic-wupstream-uinput.rules`](70-generic-wupstream-uinput.rules) y
ejecuta lo siguiente desde la carpeta donde la guardaste:

```sh
sudo install -m 644 70-generic-wupstream-uinput.rules /etc/udev/rules.d/
printf 'uinput\n' | sudo tee /etc/modules-load.d/generic-wupstream.conf
```

Reinicia y ejecuta la app como usuario normal. Puedes comprobar el permiso con
`test -w /dev/uinput && echo "uinput disponible"`. Si el mando virtual no está
disponible, todavía puedes conectar con **Enviar pantalla** activado.
