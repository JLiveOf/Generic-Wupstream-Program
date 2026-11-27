# SPDX-License-Identifier: GPL-3.0-or-later
import ctypes
import os
import subprocess
import tempfile
import uuid
import wave


class PadAudio:
    def __init__(self, env, tr):
        self.env, self.tr = env, tr
        self.capture = self.mic_fd = self.mic_dir = self.recording = self.module = None
        self.partial = b""
        self.messages = []
        self.level = 0
        self.samples = (ctypes.c_int16 * 2048)()

    def pactl(self, *args):
        return subprocess.check_output(["pactl", *args], env=self.env, text=True,
                                       stderr=subprocess.DEVNULL, timeout=3).strip()

    def open(self, desktop, virtual_mic, path):
        pcm = ["--raw", "--format=s16le", "--rate=48000"]
        if desktop:
            try:
                sink = self.pactl("get-default-sink")
                self.capture = subprocess.Popen(
                    ["parec", *pcm, "--channels=2", "--latency-msec=35", "--device=" + sink + ".monitor"],
                    env=self.env, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
                os.set_blocking(self.capture.stdout.fileno(), False)
            except (OSError, subprocess.SubprocessError) as error:
                self.messages.append(self.tr("Audio del PC no disponible: {error}", error=error))
        if virtual_mic:
            try:
                name = "wupstream_mic_" + uuid.uuid4().hex[:8]
                self.mic_dir = tempfile.TemporaryDirectory(prefix="wupstream-mic-", dir="/tmp")
                fifo = os.path.join(self.mic_dir.name, "samples")
                os.mkfifo(fifo, 0o600)
                self.module = self.pactl("load-module", "module-pipe-source", "file=" + fifo,
                                         "source_name=" + name, "format=s16le", "rate=48000", "channels=1",
                                         "source_properties=device.description=Wupstream_GamePad_Microphone")
                self.mic_fd = os.open(fifo, os.O_RDWR | os.O_NONBLOCK)
            except (OSError, subprocess.SubprocessError) as error:
                self.messages.append(self.tr("Micrófono virtual no disponible: {error}", error=error))
        if path:
            try:
                self.recording = wave.open(path, "wb")
                self.recording.setparams((1, 2, 48000, 0, "NONE", "not compressed"))
            except (OSError, wave.Error) as error:
                self.messages.append(self.tr("No se pudo abrir el WAV: {error}", error=error))

    def pump(self, lib, pad, microphone):
        if self.capture:
            try:
                data = os.read(self.capture.stdout.fileno(), 8192)
                if not data:
                    raise OSError("parec")
            except BlockingIOError:
                pass
            except OSError:
                self.stop(self.capture)
                self.capture = None
                self.messages.append(self.tr("Se perdió la salida de audio del PC."))
            else:
                data = self.partial + data
                end = len(data) // 4 * 4
                self.partial = data[end:]
                if end:
                    lib.wupstream_audio(pad, data[:end], end // 4)
        if not microphone:
            return
        count = lib.wupstream_mic(pad, self.samples, len(self.samples))
        self.level = self.level * .95
        if count <= 0:
            return
        data = ctypes.string_at(self.samples, count * 2)
        self.level = max(self.level, max(abs(value) for value in self.samples[:count]) / 32768)
        if self.mic_fd is not None:
            try:
                os.write(self.mic_fd, data)
            except BlockingIOError:
                pass
            except OSError:
                os.close(self.mic_fd)
                self.mic_fd = None
                self.messages.append(self.tr("Se perdió la salida del micrófono virtual."))
        if self.recording:
            try:
                self.recording.writeframesraw(data)
            except (OSError, wave.Error) as error:
                self.messages.append(self.tr("Se interrumpió la grabación WAV: {error}", error=error))
                self.close_recording()

    @staticmethod
    def stop(process):
        if process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=1)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
        if process.stdin:
            process.stdin.close()
        if process.stdout:
            process.stdout.close()

    def close_recording(self):
        if self.recording:
            try:
                self.recording.close()
            except (OSError, wave.Error) as error:
                self.messages.append(self.tr("No se pudo cerrar el WAV: {error}", error=error))
            self.recording = None

    def close(self):
        self.close_recording()
        if self.capture:
            self.stop(self.capture)
        if self.mic_fd is not None:
            os.close(self.mic_fd)
        if self.module:
            try:
                self.pactl("unload-module", self.module)
            except (OSError, subprocess.SubprocessError) as error:
                self.messages.append(self.tr("No se pudo quitar el micrófono virtual: {error}", error=error))
        if self.mic_dir:
            self.mic_dir.cleanup()
