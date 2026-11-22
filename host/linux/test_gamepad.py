import os
import select
import unittest
from contextlib import closing
from unittest.mock import patch

from evdev import InputDevice, ecodes as e
from wup_gamepad import Gamepad, PadInput


class MappingTest(unittest.TestCase):
    @patch("wup_gamepad.UInput")
    def test_mapping_and_neutral(self, uinput):
        controller = Gamepad(xbox_positions=True)
        device = uinput.return_value
        controller.report(PadInput(buttons=0x4000 | 0x200 | 0x80 | 0x10000, ly=1100, rx=-2000))
        for event in [(e.EV_KEY, e.BTN_SOUTH, 1), (e.EV_KEY, e.BTN_THUMBL, 1),
                      (e.EV_ABS, e.ABS_HAT0Y, -1), (e.EV_ABS, e.ABS_Z, 255),
                      (e.EV_ABS, e.ABS_Y, -32767), (e.EV_ABS, e.ABS_RX, -32767)]:
            device.write.assert_any_call(*event)
        count = device.write.call_count
        controller.report(PadInput(buttons=0x4000 | 0x200 | 0x80 | 0x10000, ly=1100, rx=-2000))
        self.assertEqual(count, device.write.call_count)
        controller.close()
        device.write.assert_any_call(e.EV_KEY, e.BTN_SOUTH, 0)
        device.write.assert_any_call(e.EV_ABS, e.ABS_Z, 0)
        device.close.assert_called_once()

    @patch("wup_gamepad.UInput")
    def test_printed_letters(self, uinput):
        controller = Gamepad(xbox_positions=False)
        controller.report(PadInput(buttons=0x8000))
        uinput.return_value.write.assert_called_once_with(e.EV_KEY, e.BTN_SOUTH, 1)

    @unittest.skipUnless(os.access("/dev/uinput", os.W_OK), "kernel uinput unavailable")
    def test_kernel_device(self):
        controller = Gamepad(xbox_positions=True)
        try:
            with closing(InputDevice(controller.device.device.path)) as reader:
                controller.report(PadInput(buttons=0x4000, lx=1100))
                self.assertTrue(select.select([reader.fd], [], [], 2)[0])
                events = {(event.type, event.code, event.value) for event in reader.read()}
                self.assertIn((e.EV_KEY, e.BTN_SOUTH, 1), events)
                self.assertIn((e.EV_ABS, e.ABS_X, 32767), events)
        finally:
            controller.close()


if __name__ == "__main__":
    unittest.main(verbosity=2)
