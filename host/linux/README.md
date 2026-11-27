# Generic Wupstream Program para Linux

[English guide](README.en.md)

## Requisitos

| Uso | Requisitos |
| --- | --- |
| Conectar el GamePad | PC Linux x86-64 y GamePad en la misma red. Abre el receptor oficial **WUP Stream** en el Homebrew Menu. |
| Paquete `.deb` | Debian, Ubuntu, Mint o compatible con APT. `apt install ./archivo.deb` instala las dependencias disponibles en los repositorios del sistema, entre ellas Python 3, GTK 3, evdev y GStreamer. |
| AppImage | Linux x86-64 con glibc 2.31 o posterior y una sesión gráfica. Incluye Python, GTK, evdev y la captura X11. Para Wayland usa `gst-launch-1.0` y el complemento `pipewiresrc` del sistema. Se extrae automáticamente al abrirlo, sin FUSE. |
| Captura en X11 | Una sesión gráfica X11. |
| Captura en Wayland | PipeWire y `xdg-desktop-portal` con un backend que implemente ScreenCast para tu compositor. El selector de pantalla lo muestra el sistema. |
| Audio del PC y micrófono virtual | Servidor compatible con PulseAudio, incluido PipeWire con `pipewire-pulse`, y los comandos `pactl` y `parec`. |
| Mando virtual | Módulo del kernel `uinput` y permiso de escritura a `/dev/uinput` en la sesión del usuario. El `.deb` instala la configuración necesaria. Si usas solo el AppImage, consulta los pasos de abajo. |

## Instalación con el `.deb`

Instala el paquete x86-64 con `sudo apt install ./generic-wupstream_2.2.5+experimental.1_amd64.deb`.
Reinicia Linux tras la instalación para activar el módulo y la regla de acceso a `/dev/uinput` para la sesión local activa.
Abre **Generic Wupstream Program** desde el lanzador o ejecuta `generic-wupstream`.

En el Homebrew Menu del GamePad, instala **WUP Stream** desde la App Store y
ábrelo. Introduce la IP que aparece en el GamePad y elige FPS y calidad.
El paquete usa H.264 a 854 × 480 y ofrece de 15 a 60 FPS de captura.
En Wayland, los FPS seleccionados son un máximo. Activa **Enviar audio del PC**
para escuchar en el GamePad la salida predeterminada de Linux. **Usar micrófono**
habilita el medidor y permite grabar un WAV mono a 48 kHz. Con **Crear micrófono
virtual**, selecciona **Wupstream GamePad Microphone** como entrada
en la aplicación que quieras usar.

Al conectar aparece **Generic Wupstream Gamepad**, un mando virtual Linux con botones, cruceta, gatillos y dos sticks. En Steam, abre **Configuración → Mando** para comprobar los botones. Activa **ABXY por posición Xbox** para que las posiciones coincidan con un mando Xbox, o desactívalo para seguir las letras del GamePad.

Desmarca **Enviar pantalla** para mostrar una imagen de **Modo mando** sin
compartir el escritorio. El mando virtual, el audio y el micrófono se pueden usar
en ese modo.

Usa **Español / English** en la esquina superior derecha para cambiar el idioma. El panel **Controles en directo** muestra los botones, sticks y batería. Puedes dejar la IP vacía si el anuncio UDP del GamePad llega al PC.

En X11 puedes elegir el escritorio completo o un monitor. En Wayland, elige un monitor en el selector
del sistema. La sesión Wayland necesita PipeWire y un portal ScreenCast
compatible con el compositor, como `xdg-desktop-portal-gnome`,
`xdg-desktop-portal-kde` o `xdg-desktop-portal-wlr` en escritorios wlroots.
**Táctil como ratón** requiere un backend de escritorio remoto con puntero en
Wayland. Activa **Permitir la interacción remota** en el selector cuando aparezca.

**Reportar un problema en GitHub** abre el formulario con un diagnóstico breve. Revisa los datos y pulsa **Submit new issue** en GitHub para publicarlos.

## AppImage

Para probar el AppImage en un Linux x86-64 con glibc 2.31 o posterior:

```sh
chmod +x Generic-Wupstream-Program-v2.2.5-x86_64.AppImage
./Generic-Wupstream-Program-v2.2.5-x86_64.AppImage
```

### Qué es FUSE

FUSE (*Filesystem in Userspace*) permite que un programa de Linux monte un
sistema de archivos. Un AppImage convencional lo usa para leer su contenido
comprimido directamente. El AppImage de la versión 2.2.5 se descomprime
automáticamente en un directorio temporal al abrirlo. Puedes iniciarlo con
doble clic después de darle permiso de ejecución, sin instalar FUSE ni añadir
opciones al comando. [Documentación oficial de AppImage sobre FUSE](https://docs.appimage.org/user-guide/troubleshooting/fuse.html).

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
