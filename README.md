# Generic Wupstream Program

[English guide](README.en.md)

Conecta un Wii U GamePad WUP-010 a Windows por la red local. Envía la pantalla y el audio del PC al GamePad y usa sus controles como un mando Xbox 360 virtual.

El selector **Español / English** cambia el idioma de la app antes de conectar.

## Conectar

1. En el GamePad, abre el **Homebrew Menu**, conéctalo al Wi‑Fi desde **Settings** e instala **WUP Stream** desde la **App Store**. Si aún no tienes el menú, sigue la [guía del autor](https://github.com/ravensclawx/wup-homebrew#get-it). El método habitual usa una Wii U con Aroma y el firmware 7677 del GamePad.
2. Abre el **WUP Stream** oficial hasta que aparezca **Waiting for a Stream** y apunta la dirección IP. El GamePad y el PC deben estar en la misma red.
3. En Windows 10 u 11 x64 con .NET Framework 4.8 y [ViGEmBus](https://github.com/nefarius/ViGEmBus/releases/latest), ejecuta el instalador de [Releases](https://github.com/JLiveOf/Generic-Wupstream-Program/releases) o extrae el ZIP portátil. Abre **Generic Wupstream Program**, selecciona el monitor, introduce la IP del GamePad y pulsa **Conectar por IP**. Puedes dejar el campo vacío para buscar el GamePad por UDP 50001. Si Windows pide permiso para la búsqueda, selecciona la red privada.

El instalador en español e inglés instala la app para el usuario actual, crea un acceso en Inicio y añade una entrada de desinstalación. ViGEmBus se obtiene por separado. Cierra la app antes de actualizarla.

Si el enlace se interrumpe, la app suelta los controles, pausa el vídeo e intenta recuperar la sesión durante 20 segundos en la misma IP. Al volver el GamePad, el vídeo arranca con un fotograma clave. El vídeo empieza con un límite de 3,2 Mb/s y lo ajusta según las solicitudes de reenvío. La ventana muestra ambos valores durante la transmisión. El sonido también ocupa ancho de banda además del vídeo.

## Controles y multimedia

- **Enviar pantalla** captura el monitor seleccionado y mantiene su proporción en los 854 × 480 píxeles del GamePad. La captura funciona con aplicaciones normales y juegos en ventana o sin bordes. El escritorio seguro, el contenido protegido y algunos juegos a pantalla completa exclusiva pueden aparecer negros.
- Elige **FPS** (de 15 a 60 fps de captura) y **Calidad** antes de conectar. El emisor repite fotogramas para mantener el ritmo de la pantalla del GamePad. La calidad baja, media y alta limita el bitrate a 1,8, 3,2 y 5,5 Mb/s, respectivamente.
- Desactiva **Enviar pantalla** para que el GamePad muestre **Modo mando** sin capturar el escritorio.
- **Enviar audio del PC** copia el sonido de la salida predeterminada de Windows al GamePad. El audio sigue activo también en el PC. El volumen del GamePad se regula con su deslizador.
- **Táctil como ratón** activa clic izquierdo y arrastre con el lápiz o el dedo en la imagen del monitor. Se puede activar y desactivar durante la conexión. Está desactivado al iniciar la aplicación.
- **Probar vibración** activa el motor durante un tercio de segundo. Los juegos que usan la vibración del mando virtual también pueden activarlo. El selector **Brillo** ajusta la pantalla durante la conexión.
- **Usar micrófono** activa la recepción y el medidor de nivel del micrófono del GamePad. Para grabarlo, pulsa **Grabar WAV…** y elige el archivo antes de conectar. Se guarda en PCM mono a 48 kHz y se finaliza al desconectar.
- Para usar el micrófono en Discord, OBS u otra aplicación, instala [VB-CABLE](https://vb-audio.com/Cable/), activa **Usar micrófono** y **Enviar a VB-CABLE**, y selecciona **CABLE Output** como entrada en la otra aplicación. Mantén tus altavoces o auriculares como salida predeterminada de Windows para seguir enviando el audio del PC al GamePad.
- **Probar en Windows** abre el panel de mandos. Deja los sticks en reposo al conectar para que se calibren. De forma predeterminada, A, B, X e Y siguen las letras impresas. Activa **ABXY por posición Xbox** para intercambiar A/B y X/Y y que los botones coincidan con las posiciones de un mando Xbox. Puedes cambiarlo mientras juegas. **+** es Start, **−** es Back y **HOME** es Guide. La ventana muestra valores de acelerómetro y giroscopio para diagnóstico. El mando XInput solo transporta botones, sticks, gatillos y vibración.

La cámara, el NFC/Amiibo y el infrarrojo requieren soporte adicional en el software del GamePad. WUP Stream no transmite esos periféricos. El enlace usa UDP sin cifrar en la red local.

## Linux

La [guía del paquete Linux](host/linux/README.md) cubre la captura en X11 y Wayland.

## Compilar y licencias

En Windows x64, instala .NET Framework 4.8, Git para Windows y un compilador MinGW-w64 x64 con Make, como [w64devkit](https://github.com/skeeto/w64devkit). Añade [NASM](https://www.nasm.us/) al PATH para compilar x264 con SIMD. Desde la raíz del código, ejecuta en PowerShell:

    .\host\windows\Build.ps1 -ToolchainBin 'C:\ruta\a\w64devkit\bin'

Los binarios aparecen en `build/windows`. La compilación de Windows exige x264 con SIMD porque, sin esa optimización, el codificador puede perder plazos y bloquear el decodificador del GamePad. El script obtiene la revisión fijada de x264, le aplica el parche DRH de wup-stream y verifica el hash del paquete Nefarius.ViGEm.Client. Si tu copia de Git Bash está en otra ubicación, indícala con `-GitBash`.

Para generar el instalador después de compilar la app, ejecuta `host/windows/BuildSetup.ps1` con [NSIS 3](https://nsis.sourceforge.io/Download) en el PATH o indica su compilador mediante `-MakeNsis`.

El cliente y las modificaciones de la biblioteca son GPL-3.0-or-later. Basado en [wup-stream de ravensclawx](https://github.com/ravensclawx/wup-stream/tree/790aef47a28146f01a535ab1afdc7990ddcb2dfb). El motor incluye [x264](https://github.com/mirror/x264/tree/b35605ace3ddf7c1a5d67a2eb553f034aef41d55) bajo licencia GPL. Nefarius.ViGEm.Client se distribuye bajo licencia MIT.
