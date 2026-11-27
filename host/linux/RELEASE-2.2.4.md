## Español

Prerelease de Linux x86-64 para el receptor oficial WUP Stream.

- Corrige el cierre del AppImage al abrirlo en Wayland con esquemas recientes de GNOME ([#3](https://github.com/JLiveOf/Generic-Wupstream-Program/issues/3)).
- El AppImage se extrae automáticamente y funciona sin FUSE. Dale permiso de ejecución antes de abrirlo.
- El paquete .deb instala sus dependencias con APT. Para usar el mando virtual con el AppImage, comprueba el acceso a `/dev/uinput`.

[Guía y requisitos en español](https://github.com/JLiveOf/Generic-Wupstream-Program/blob/v2.2.4-linux.1/host/linux/README.md)

## English

Linux x86-64 prerelease for the official WUP Stream receiver.

- Fixes an AppImage startup crash on Wayland with newer GNOME schemas ([#3](https://github.com/JLiveOf/Generic-Wupstream-Program/issues/3)).
- The AppImage extracts itself on launch and runs without FUSE. Mark it executable before opening it.
- APT installs the .deb dependencies. For virtual controller support with the AppImage, check access to `/dev/uinput`.

[English guide and requirements](https://github.com/JLiveOf/Generic-Wupstream-Program/blob/v2.2.4-linux.1/host/linux/README.en.md)
