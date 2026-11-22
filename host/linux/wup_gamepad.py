# SPDX-License-Identifier: GPL-3.0-or-later
import ctypes

from evdev import AbsInfo, UInput, ecodes as e


class PadInput(ctypes.Structure):
    _fields_ = [("buttons", ctypes.c_uint32),
                *[(axis, ctypes.c_int) for axis in ("lx", "ly", "rx", "ry")],
                *[(field, ctypes.c_int) for field in ("touched", "tx", "ty")],
                ("accel", ctypes.c_int * 3), ("gyro", ctypes.c_int * 3),
                *[(field, ctypes.c_int) for field in ("volume", "backlight", "battery", "charging", "plugged")],
                ("report", ctypes.c_uint8 * 128)]


class Gamepad:
    def __init__(self, xbox_positions):
        face = (e.BTN_EAST, e.BTN_SOUTH, e.BTN_NORTH, e.BTN_WEST) if xbox_positions else (
            e.BTN_SOUTH, e.BTN_EAST, e.BTN_WEST, e.BTN_NORTH)
        self.keys = dict(zip((0x8000, 0x4000, 0x2000, 0x1000), face))
        self.keys.update({0x20: e.BTN_TL, 0x10: e.BTN_TR, 8: e.BTN_START, 4: e.BTN_SELECT,
                          2: e.BTN_MODE, 0x10000: e.BTN_THUMBL, 0x20000: e.BTN_THUMBR})
        stick = AbsInfo(0, -32767, 32767, 16, 512, 0)
        hat = AbsInfo(0, -1, 1, 0, 0, 0)
        trigger = AbsInfo(0, 0, 255, 0, 0, 0)
        axes = [(code, stick) for code in (e.ABS_X, e.ABS_Y, e.ABS_RX, e.ABS_RY)]
        axes += [(e.ABS_HAT0X, hat), (e.ABS_HAT0Y, hat), (e.ABS_Z, trigger), (e.ABS_RZ, trigger)]
        self.device = UInput({e.EV_KEY: list(self.keys.values()), e.EV_ABS: axes},
                             name="Generic Wupstream Gamepad", bustype=e.BUS_VIRTUAL,
                             vendor=0x1209, product=0x0001, version=1)
        self.previous = {}

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

    def close(self):
        try:
            self.report(PadInput())
        finally:
            self.device.close()
