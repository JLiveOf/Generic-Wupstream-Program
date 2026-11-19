// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.Generic;

namespace WupPad
{
    internal static class Language
    {
        internal static volatile bool English;

        private static readonly Dictionary<string, string> Translations = new Dictionary<string, string>
        {
            { "Abre WUP Stream en el GamePad y conéctalo a la misma red que el PC.", "Open WUP Stream on the GamePad and connect it to the same network as the PC." },
            { "IP del GamePad  (opcional)", "GamePad IP  (optional)" },
            { "IP del GamePad, opcional", "GamePad IP, optional" },
            { "Buscar GamePad", "Find GamePad" },
            { "Conectar por IP", "Connect by IP" },
            { "Desconectar", "Disconnect" },
            { "Desconectando…", "Disconnecting…" },
            { "Cancelar", "Cancel" },
            { "Estado de la conexión", "Connection status" },
            { "Introduce la IP de WUP Stream o deja el campo vacío para buscarlo.", "Enter the IP shown in WUP Stream or leave it blank to search." },
            { "Monitor", "Display" },
            { "Principal", "Primary" },
            { "Enviar pantalla", "Stream screen" },
            { "Enviar audio del PC", "Stream PC audio" },
            { "Táctil como ratón", "Use touch as mouse" },
            { "Usar micrófono", "Enable microphone" },
            { "Enviar a VB-CABLE", "Send to VB-CABLE" },
            { "Grabar WAV…", "Record WAV…" },
            { "Quitar grabación WAV", "Remove WAV recording" },
            { "Probar vibración", "Test vibration" },
            { "Brillo", "Brightness" },
            { "Brillo del GamePad", "GamePad brightness" },
            { "Del GamePad", "GamePad default" },
            { "Volumen del GamePad · —", "GamePad volume · —" },
            { "Volumen · ", "Volume · " },
            { "Nivel del micrófono", "Microphone level" },
            { "Acelerómetro · ", "Accelerometer · " },
            { "     Giroscopio · ", "     Gyroscope · " },
            { "Acelerómetro · —     Giroscopio · —", "Accelerometer · —     Gyroscope · —" },
            { "Controles en directo", "Live controls" },
            { "Batería · ", "Battery · " },
            { "Batería · —", "Battery · —" },
            { "  ·  Cargando", "  ·  Charging" },
            { "Probar en Windows", "Test in Windows" },
            { "ABXY por posición Xbox", "Xbox ABXY positions" },
            { "Usa las posiciones de Xbox: B→A, A→B, Y→X, X→Y.", "Use Xbox button positions: B→A, A→B, Y→X, X→Y." },
            { "Instalar Homebrew Menu", "Install Homebrew Menu" },
            { "Guardar el micrófono del GamePad", "Record the GamePad microphone" },
            { "No se pudo abrir", "Could not open" },
            { "Escribe la dirección IPv4 que aparece en WUP Stream.", "Enter the IPv4 address shown in WUP Stream." },
            { "Iniciando conexión…", "Starting connection…" },
            { "GamePad desconectado.", "GamePad disconnected." },
            { "Windows no encuentra ViGEmBus. Consulta la guía incluida con la app.", "Windows could not find ViGEmBus. See the included guide." },
            { "Error de red ({0}). Comprueba la conexión y la IP del GamePad.", "Network error ({0}). Check the connection and the GamePad IP." },
            { "Falta WupStreamNative.dll. Extrae todos los archivos del ZIP juntos.", "WupStreamNative.dll is missing. Extract all the files from the ZIP together." },
            { "Estado de botones y sticks", "Button and stick status" },
            { "Izquierdo", "Left stick" },
            { "Derecho", "Right stick" },
            { "Buscando GamePad en la red…", "Searching the local network for a GamePad…" },
            { "Sincronizando con {0}…", "Synchronizing with {0}…" },
            { "No se pudo sincronizar con {0}. Abre WUP Stream y revisa su IP.", "Could not synchronize with {0}. Open WUP Stream and check its IP." },
            { "Conectado · ", "Connected · " },
            { "Reconectando con {0} · {1} s", "Reconnecting to {0} · {1} s" },
            { "  ·  Límite {0:0.0} Mb/s  ·  Solicitudes de reenvío {1:0.0} %", "  ·  Cap {0:0.0} Mb/s  ·  Retransmission requests {1:0.0} %" },
            { "Generic Wupstream Program ya está abierto.", "Generic Wupstream Program is already open." },
            { "Se perdió la conexión con el GamePad.", "The connection to the GamePad was lost." },
            { "No se puede abrir UDP 50001. Puedes conectar con la IP del GamePad.", "Could not open UDP 50001. Connect using the GamePad IP." },
            { "No se encontró el GamePad. Prueba con la IP que muestra WUP Stream.", "GamePad not found. Try the IP shown in WUP Stream." },
            { "Windows no permitió capturar el escritorio.", "Windows did not allow desktop capture." },
            { "La captura del monitor se interrumpió.", "Display capture was interrupted." },
            { "Audio de PC no disponible (0x", "PC audio unavailable (0x" },
            { "Audio de PC → GamePad", "PC audio → GamePad" },
            { "VB-CABLE no disponible (0x", "VB-CABLE unavailable (0x" },
            { "Micrófono → CABLE Output", "Microphone → CABLE Output" },
            { "Audio de PC pausado: CABLE Input es la salida predeterminada", "PC audio paused because CABLE Input is the default output" },
            { "Micrófono activo", "Microphone enabled" },
            { "Grabando WAV", "Recording WAV" },
            { "No se pudo abrir el WAV", "Could not open the WAV file" },
            { "No hay permiso para guardar el WAV", "No permission to save the WAV file" },
            { "Se perdió la salida de audio del PC", "PC audio output was lost" },
            { "Se perdió la salida hacia VB-CABLE", "VB-CABLE output was lost" },
            { "Se interrumpió la grabación WAV", "WAV recording was interrupted" },
            { "No se pudo cerrar el WAV", "Could not finalize the WAV file" },
            { "El WAV llegó al límite RIFF.", "The WAV file reached the RIFF size limit." }
        };

        internal static string Get(string spanish)
        {
            string english;
            return English && Translations.TryGetValue(spanish, out english) ? english : spanish;
        }
    }
}
