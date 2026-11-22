# SPDX-License-Identifier: GPL-3.0-or-later
from math import hypot, tau

from gi.repository import Gtk

ENGLISH = {
    "Abre WUP Stream en el GamePad y conéctalo a la misma red que el PC.": "Open WUP Stream on the GamePad and connect it to the same network as the PC.",
    "IP del GamePad (opcional)": "GamePad IP (optional)",
    "Introduce la IP de WUP Stream o deja el campo vacío para buscarlo.": "Enter the IP shown in WUP Stream or leave it blank to search.",
    "Buscar GamePad": "Find GamePad", "Conectar por IP": "Connect by IP", "Desconectar": "Disconnect",
    "Desconectando…": "Disconnecting…", "Desconectado": "Disconnected",
    "Enviar pantalla": "Stream screen", "Captura": "Capture", "Automática": "Automatic",
    "Calidad": "Quality", "Baja": "Low", "Media": "Medium", "Alta": "High",
    "Controles en directo": "Live controls", "Batería · —": "Battery · —",
    "Batería · {level} %": "Battery · {level}%", " · Cargando": " · Charging",
    "Izquierdo": "Left stick", "Derecho": "Right stick",
    "Reportar un problema en GitHub": "Report an issue on GitHub",
    "ABXY por posición Xbox": "Xbox ABXY positions",
    "Revisa el diagnóstico antes de abrir el issue": "Review the diagnostics before opening the issue",
    "Cancelar": "Cancel", "Abrir GitHub": "Open GitHub",
    "No se pudo abrir el navegador.": "Could not open the browser.",
    "Qué ocurrió": "What happened", "Cómo reproducirlo": "Steps to reproduce",
    "Diagnóstico de Generic Wupstream": "Generic Wupstream diagnostics",
    "Esperando al WUP Stream oficial…": "Waiting for the official WUP Stream receiver…",
    "Escribe la dirección IPv4 que aparece en WUP Stream.": "Enter the IPv4 address shown in WUP Stream.",
    "No se pudo conectar. Abre el WUP Stream oficial y verifica su IP.": "Could not connect. Open the official WUP Stream receiver and check its IP.",
    "No se pudo iniciar la captura.": "Could not start screen capture.",
    "Terminó la captura de pantalla.": "Screen capture ended.",
    "La captura devolvió un formato de imagen inesperado.": "Screen capture returned an unexpected image format.",
    "Se perdió la conexión con el GamePad.": "Lost the connection to the GamePad.",
    "El portal devolvió una ruta de solicitud inesperada.": "The screen capture portal returned an unexpected request path.",
    "El portal de captura no respondió.": "The screen capture portal did not respond.",
    "Se canceló la captura de pantalla.": "Screen capture was canceled.",
    "No se pudo crear el mando virtual en /dev/uinput. Comprueba el acceso desde tu sesión de Linux.": "Could not create the virtual controller in /dev/uinput. Check access from your Linux session.",
    "Conectado · Mando virtual": "Connected · Virtual controller",
    "Conectado · Mando virtual y vídeo": "Connected · Virtual controller and video",
    "Conectado · Vídeo (sin mando virtual)": "Connected · Video (virtual controller unavailable)",
    "Mando conectado · Error de vídeo: {error}": "Controller connected · Video error: {error}",
    "Error de vídeo: {error}": "Video error: {error}",
    "Reconectando · Mando en reposo": "Reconnecting · Controller neutral",
}


def translate(message, english, **values):
    return (ENGLISH.get(message, message) if english else message).format(**values)


CSS = b"""
window.wup, window.wup dialog { background: #11161f; color: #eef4fc; }
.wup label { color: #eef4fc; font-size: 13px; }
.wup .title { font-size: 33px; font-weight: 600; }
.wup .muted { color: #a3b5cc; }
.wup .accent-text { color: #67ead0; }
.wup .section { font-size: 17px; font-weight: 600; }
.wup button, .wup entry { color: #eef4fc; background: #1c2431; background-image: none;
                          border: 1px solid #354458; border-radius: 6px; box-shadow: none;
                          min-height: 0; padding: 3px 8px; }
.wup button:hover, .wup entry:focus { border-color: #67ead0; }
.wup button:disabled { color: #a3b5cc; opacity: 0.5; }
.wup button.primary { color: #11161f; background: #67ead0; border-color: #67ead0; }
.wup button.primary label { color: #11161f; }
.wup button.primary:hover { background: #81edda; }
.wup checkbutton check { color: #11161f; background: #11161f; border-color: #a3b5cc; }
.wup checkbutton check:checked { background: #67ead0; border-color: #67ead0; }
"""


class PadView(Gtk.DrawingArea):
    NAMES = ("A", "B", "X", "Y", "L", "R", "ZL", "ZR", "↑", "↓", "←", "→", "+", "−", "L3", "R3", "HOME")
    MASKS = (0x8000, 0x4000, 0x2000, 0x1000, 0x20, 0x10, 0x80, 0x40,
             0x200, 0x100, 0x800, 0x400, 8, 4, 0x10000, 0x20000, 2)
    PANEL, EDGE, DARK, MUTED, ACCENT = ("1c2431", "354458", "11161f", "a3b5cc", "67ead0")

    def __init__(self, tr):
        super().__init__()
        self.tr = tr
        self.state = None
        self.set_size_request(652, 196)
        self.connect("draw", self.paint)

    @staticmethod
    def color(cr, hex_color):
        cr.set_source_rgb(*(int(hex_color[i:i + 2], 16) / 255 for i in (0, 2, 4)))

    def caption(self, cr, text, x, y, color):
        self.color(cr, color)
        xb, yb, width, height, *_ = cr.text_extents(text)
        cr.move_to(x - xb - width / 2, y - yb - height / 2)
        cr.show_text(text)

    def paint(self, _, cr):
        cr.scale(self.get_allocated_width() / 652, self.get_allocated_height() / 196)
        cr.set_line_width(1)
        self.color(cr, self.PANEL)
        cr.paint()
        self.color(cr, self.EDGE)
        cr.rectangle(.5, .5, 651, 195)
        cr.move_to(252, 18)
        cr.line_to(252, 178)
        cr.stroke()
        cr.select_font_face("Sans")
        cr.set_font_size(13)
        buttons, lx, ly, rx, ry = self.state or (0, 0, 0, 0, 0)
        for x, ax, ay, label in ((70, lx, ly, "Izquierdo"), (181, rx, ry, "Derecho")):
            self.color(cr, self.EDGE)
            cr.new_sub_path()
            cr.arc(x, 80, 41, 0, tau)
            cr.stroke()
            cr.move_to(x - 40, 80)
            cr.line_to(x + 40, 80)
            cr.move_to(x, 40)
            cr.line_to(x, 120)
            cr.stroke()
            scale = max(1100, hypot(ax, ay))
            self.color(cr, self.ACCENT if self.state else self.MUTED)
            cr.new_sub_path()
            cr.arc(x + 37 * ax / scale, 80 - 37 * ay / scale, 6, 0, tau)
            cr.fill()
            self.caption(cr, self.tr(label), x, 152, self.MUTED)
        for i, (name, mask) in enumerate(zip(self.NAMES, self.MASKS)):
            x, y = 271 + i % 6 * 61, 21 + i // 6 * 54
            held = bool(buttons & mask)
            self.color(cr, self.ACCENT if held else self.DARK)
            cr.rectangle(x + .5, y + .5, 54, 44)
            cr.fill_preserve()
            self.color(cr, self.EDGE)
            cr.stroke()
            self.caption(cr, name, x + 27, y + 22, self.DARK if held else self.MUTED)
        return False
