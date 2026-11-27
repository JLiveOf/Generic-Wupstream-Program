# SPDX-License-Identifier: GPL-3.0-or-later
import ctypes
import fcntl

from evdev import AbsInfo, UInput, ecodes as e, ff


class PadInput(ctypes.Structure):
    _fields_ = [("buttons", ctypes.c_uint32),
                *[(axis, ctypes.c_int) for axis in ("lx", "ly", "rx", "ry")],
                *[(field, ctypes.c_int) for field in ("touched", "tx", "ty")],
                ("accel", ctypes.c_int * 3), ("gyro", ctypes.c_int * 3),
                *[(field, ctypes.c_int) for field in ("volume", "backlight", "battery", "charging", "plugged")],
                ("report", ctypes.c_uint8 * 128)]


class Gamepad:
    def __init__(self, xbox_positions):
        self.keys = {0x20: e.BTN_TL, 0x10: e.BTN_TR, 8: e.BTN_START, 4: e.BTN_SELECT,
                     2: e.BTN_MODE, 0x10000: e.BTN_THUMBL, 0x20000: e.BTN_THUMBR}
        self.set_xbox_positions(xbox_positions)
        stick = AbsInfo(0, -32767, 32767, 16, 512, 0)
        hat = AbsInfo(0, -1, 1, 0, 0, 0)
        trigger = AbsInfo(0, 0, 255, 0, 0, 0)
        axes = [(code, stick) for code in (e.ABS_X, e.ABS_Y, e.ABS_RX, e.ABS_RY)]
        axes += [(e.ABS_HAT0X, hat), (e.ABS_HAT0Y, hat), (e.ABS_Z, trigger), (e.ABS_RZ, trigger)]
        self.device = UInput({e.EV_KEY: list(self.keys.values()), e.EV_ABS: axes, e.EV_FF: [e.FF_RUMBLE]},
                             name="Generic Wupstream Gamepad", bustype=e.BUS_VIRTUAL,
                             vendor=0x1209, product=0x0001, version=1)
        self.previous = {}
        self.effects = {}
        self.playing = {}

    def set_xbox_positions(self, xbox_positions):
        self.xbox_positions = xbox_positions
        face = (e.BTN_EAST, e.BTN_SOUTH, e.BTN_NORTH, e.BTN_WEST) if xbox_positions else (
            e.BTN_SOUTH, e.BTN_EAST, e.BTN_WEST, e.BTN_NORTH)
        self.keys.update(zip((0x8000, 0x4000, 0x2000, 0x1000), face))

    @staticmethod
    def stick(value):
        if abs(value) < 24:
            return 0
        return max(-32767, min(32767, round(value * 32767 / 1100)))

    def report(self, state):
        buttons = state.buttons
        values = {(e.EV_KEY, code): int(bool(buttons & mask)) for mask, code in self.keys.items()}
        values.update({
            (e.EV_ABS, e.ABS_X): self.stick(state.lx),
            (e.EV_ABS, e.ABS_Y): self.stick(-state.ly),
            (e.EV_ABS, e.ABS_RX): self.stick(state.rx),
            (e.EV_ABS, e.ABS_RY): self.stick(-state.ry),
            (e.EV_ABS, e.ABS_HAT0X): int(bool(buttons & 0x400)) - int(bool(buttons & 0x800)),
            (e.EV_ABS, e.ABS_HAT0Y): int(bool(buttons & 0x100)) - int(bool(buttons & 0x200)),
            (e.EV_ABS, e.ABS_Z): 255 if buttons & 0x80 else 0,
            (e.EV_ABS, e.ABS_RZ): 255 if buttons & 0x40 else 0,
        })
        changed = [(kind, code, value) for (kind, code), value in values.items()
                   if value != self.previous.get((kind, code), 0)]
        if changed:
            for kind, code, value in changed:
                self.device.write(kind, code, value)
            self.device.syn()
        self.previous = values

    def rumble(self, now):
        try:
            events = list(self.device.read())
        except BlockingIOError:
            events = []
        for event in events:
            if event.type == e.EV_UINPUT and event.code == e.UI_FF_UPLOAD:
                upload = ff.UInputUpload()
                upload.request_id = event.value
                self.ff_ioctl(200, upload, True)
                try:
                    effect = upload.effect
                    rumble = effect.u.ff_rumble_effect
                    magnitude = rumble.strong_magnitude or rumble.weak_magnitude
                    self.effects[effect.id] = (effect.ff_replay.delay, effect.ff_replay.length, magnitude)
                    upload.retval = 0
                finally:
                    self.ff_ioctl(201, upload, False)
            elif event.type == e.EV_UINPUT and event.code == e.UI_FF_ERASE:
                erase = ff.UInputErase()
                erase.request_id = event.value
                self.ff_ioctl(202, erase, True)
                self.effects.pop(erase.effect_id, None)
                self.playing.pop(erase.effect_id, None)
                erase.retval = 0
                self.ff_ioctl(203, erase, False)
            elif event.type == e.EV_FF and event.code in self.effects:
                delay, length, magnitude = self.effects[event.code]
                if event.value > 0 and magnitude:
                    start = now + delay / 1000
                    self.playing[event.code] = start, start + max(1, length) * event.value / 1000
                else:
                    self.playing.pop(event.code, None)
        self.playing = {effect: span for effect, span in self.playing.items() if now < span[1]}
        return any(start <= now for start, _ in self.playing.values())

    def ff_ioctl(self, command, data, read):
        # Linux uinput uses the request ID, not the effect ID, for this handshake.
        direction = 3 if read else 1
        request = direction << 30 | ctypes.sizeof(data) << 16 | ord("U") << 8 | command
        fcntl.ioctl(self.device.fd, request, data, True)

    def close(self):
        try:
            self.report(PadInput())
        finally:
            self.device.close()


class X11Pointer:
    def __init__(self, x, y, width, height, screen_point):
        self.x11 = ctypes.CDLL("libX11.so.6")
        self.xtest = ctypes.CDLL("libXtst.so.6")
        self.x11.XOpenDisplay.argtypes = [ctypes.c_char_p]
        self.x11.XOpenDisplay.restype = ctypes.c_void_p
        self.x11.XFlush.argtypes = [ctypes.c_void_p]
        self.x11.XCloseDisplay.argtypes = [ctypes.c_void_p]
        self.xtest.XTestFakeMotionEvent.argtypes = [ctypes.c_void_p, ctypes.c_int, ctypes.c_int,
                                                   ctypes.c_int, ctypes.c_ulong]
        self.xtest.XTestFakeButtonEvent.argtypes = [ctypes.c_void_p, ctypes.c_uint,
                                                   ctypes.c_int, ctypes.c_ulong]
        self.display = self.x11.XOpenDisplay(None)
        if not self.display:
            raise RuntimeError("X11 display unavailable")
        self.origin = x, y
        self.size = width, height
        self.screen_point = screen_point
        self.pressed = False
        self.position = None

    def touch(self, pressed, x=0, y=0):
        changed = False
        if pressed:
            local_x, local_y = self.screen_point(x, y, *self.size)
            position = self.origin[0] + int(local_x), self.origin[1] + int(local_y)
            if position != self.position:
                self.position = position
                self.xtest.XTestFakeMotionEvent(self.display, -1, *position, 0)
                changed = True
        if pressed != self.pressed:
            self.pressed = pressed
            self.xtest.XTestFakeButtonEvent(self.display, 1, int(pressed), 0)
            changed = True
        if changed:
            self.x11.XFlush(self.display)

    def close(self):
        self.touch(False)
        self.x11.XCloseDisplay(self.display)
