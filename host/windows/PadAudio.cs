// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Text;

namespace WupPad
{
    internal sealed class PadAudio : IDisposable
    {
        private readonly bool microphone;
        private readonly short[] stereo = new short[8192];
        private readonly short[] mono = new short[2048];
        private IntPtr capture, cable;
        private WaveRecording recording;
        private string output = "", input = "", file = "";
        private long decayAt;
        internal int Level { get; private set; }

        internal PadAudio(StreamOptions options)
        {
            microphone = options.Microphone;
            int error;
            if (options.Audio)
            {
                capture = Native.audio_open(0, out error);
                output = capture == IntPtr.Zero ? Language.Get("Audio de PC no disponible (0x") + error.ToString("X8") + ")"
                    : Language.Get("Audio de PC → GamePad");
            }
            if (options.Cable)
            {
                cable = Native.audio_open(1, out error);
                input = cable == IntPtr.Zero ? Language.Get("VB-CABLE no disponible (0x") + error.ToString("X8") + ")"
                    : Language.Get("Micrófono → CABLE Output");
                if (Native.audio_same_device(capture, cable) != 0)
                {
                    Close(ref capture);
                    output = Language.Get("Audio de PC pausado: CABLE Input es la salida predeterminada");
                }
            }
            else if (microphone) input = Language.Get("Micrófono activo");

            if (options.Recording != null)
            {
                try
                {
                    recording = new WaveRecording(options.Recording);
                    file = Language.Get("Grabando WAV");
                }
                catch (IOException) { file = Language.Get("No se pudo abrir el WAV"); }
                catch (UnauthorizedAccessException) { file = Language.Get("No hay permiso para guardar el WAV"); }
            }
        }

        internal string Status
        {
            get
            {
                return string.Join("  ·  ", Array.FindAll(new[] { output, input, file }, text => text.Length != 0));
            }
        }

        internal void Pump(IntPtr pad, long now)
        {
            if (capture != IntPtr.Zero)
            {
                for (int i = 0; i < 4; i++)
                {
                    int frames = Native.audio_read(capture, stereo, stereo.Length / 2);
                    if (frames < 0) { Close(ref capture); output = Language.Get("Se perdió la salida de audio del PC"); }
                    if (frames <= 0 || Native.host_audio(pad, stereo, frames) != 0) break;
                }
            }
            if (!microphone) return;

            if (now >= decayAt)
            {
                Level = Level * 3 / 4;
                decayAt = now + 45;
            }
            int n = Native.host_mic(pad, mono, mono.Length);
            if (n <= 0) return;
            int peak = 0;
            for (int i = 0; i < n; i++) peak = Math.Max(peak, Math.Abs((int)mono[i]));
            Level = Math.Max(Level, peak * 100 / 32768);
            if (cable != IntPtr.Zero && Native.audio_write_mono(cable, mono, n) < 0)
            {
                Close(ref cable);
                input = Language.Get("Se perdió la salida hacia VB-CABLE");
            }
            if (recording != null)
            {
                try { recording.Write(mono, n); }
                catch (IOException)
                {
                    file = Language.Get("Se interrumpió la grabación WAV");
                    FinishRecording();
                }
            }
        }

        public void Dispose()
        {
            FinishRecording();
            Close(ref cable);
            Close(ref capture);
        }

        private void FinishRecording()
        {
            if (recording == null) return;
            try { recording.Dispose(); }
            catch (IOException) { file = Language.Get("No se pudo cerrar el WAV"); }
            recording = null;
        }

        private static void Close(ref IntPtr stream)
        {
            if (stream != IntPtr.Zero) Native.audio_close(stream);
            stream = IntPtr.Zero;
        }
    }

    internal sealed class WaveRecording : IDisposable
    {
        private readonly BinaryWriter writer;
        private readonly byte[] buffer = new byte[4096];
        private uint length;

        internal WaveRecording(string path)
        {
            writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read));
            try
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(48000);
                writer.Write(96000);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(0);
            }
            catch
            {
                writer.Dispose();
                throw;
            }
        }

        internal void Write(short[] samples, int count)
        {
            int bytes = count * 2;
            if (length + (long)bytes > uint.MaxValue - 36) throw new IOException(Language.Get("El WAV llegó al límite RIFF."));
            Buffer.BlockCopy(samples, 0, buffer, 0, bytes);
            writer.Write(buffer, 0, bytes);
            length += (uint)bytes;
        }

        public void Dispose()
        {
            using (writer)
            {
                writer.BaseStream.Position = 4;
                writer.Write(36 + length);
                writer.BaseStream.Position = 40;
                writer.Write(length);
            }
        }
    }
}
