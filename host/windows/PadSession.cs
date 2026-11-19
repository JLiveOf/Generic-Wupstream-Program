// SPDX-License-Identifier: GPL-3.0-or-later
// Wire format and input layout from pad/main.c and host/wupstream.c.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace WupPad
{
    internal sealed class PadState
    {
        private static readonly int[] WupButtons = {
            0x8000, 0x4000, 0x2000, 0x1000, 0x0020, 0x0010, 0x0080, 0x0040,
            0x0200, 0x0100, 0x0800, 0x0400, 0x0008, 0x0004, 0x10000, 0x20000, 0x0002
        };
        private static readonly ushort[] XboxButtons = {
            0x1000, 0x2000, 0x4000, 0x8000, 0x0100, 0x0200, 0, 0,
            0x0001, 0x0002, 0x0004, 0x0008, 0x0010, 0x0020, 0x0040, 0x0080, 0x0400
        };
        internal static readonly string[] Names = {
            "A", "B", "X", "Y", "L", "R", "ZL", "ZR", "↑", "↓", "←", "→",
            "+", "−", "L3", "R3", "HOME"
        };
        internal readonly short[] Axes = new short[4];
        internal readonly bool[] Held = new bool[Names.Length];
        internal readonly ushort Buttons;
        internal readonly int Battery;
        internal readonly bool Charging;
        internal int MicLevel, Backlight, Volume;
        internal int[] Accel, Gyro;
        internal string AudioStatus;

        internal PadState(byte[] report, int[] centers)
        {
            int flags = (report[2] << 8) | report[3];
            int extra = report[0x50];
            if ((extra & 0x80) != 0) flags |= 0x10000;
            if ((extra & 0x40) != 0) flags |= 0x20000;
            for (int i = 0; i < Held.Length; i++)
            {
                Held[i] = (flags & WupButtons[i]) != 0;
                if (Held[i]) Buttons |= XboxButtons[i];
            }
            for (int i = 0; i < Axes.Length; i++)
                Axes[i] = ScaleAxis(ReadAxis(report, i) - centers[i]);

            int raw = report[5];
            Charging = (report[4] & 0x40) != 0;
            Battery = raw == 0 ? -1 : Math.Min(100, Math.Max(0, (raw - (Charging ? 12 : 0) - 136) * 100 / 22));
        }

        internal static int ReadAxis(byte[] report, int axis)
        {
            int offset = 6 + 2 * axis;
            return (report[offset] | (report[offset + 1] << 8)) - 0x800;
        }

        private static short ScaleAxis(int raw)
        {
            const int deadZone = 100;
            int magnitude = Math.Min(1100, Math.Abs(raw));
            if (magnitude <= deadZone) return 0;
            int range = raw < 0 ? 32768 : 32767;
            return (short)(Math.Sign(raw) * (magnitude - deadZone) * range / (1100 - deadZone));
        }
    }

    internal sealed class StreamOptions
    {
        internal Rectangle Monitor;
        internal bool Video, Audio, Microphone, Cable;
        internal string Recording;
        internal volatile bool Touch;
        internal volatile int Brightness = -1;
        internal long VibrationUntil;
    }

    internal sealed class PadSession
    {
        private const int PadPort = 50000;
        private const int DiscoveryPort = 50001;
        private volatile bool rumble;
        private readonly CancellationToken stop;
        private readonly Action<string, PadState> publish;
        private readonly StreamOptions options;

        internal PadSession(CancellationToken stop, Action<string, PadState> publish, StreamOptions options)
        {
            this.stop = stop;
            this.publish = publish;
            this.options = options;
        }

        internal void Run(IPAddress address)
        {
            if (address == null)
            {
                publish(Language.Get("Buscando GamePad en la red…"), null);
                address = Discover();
            }

            publish(string.Format(Language.Get("Sincronizando con {0}…"), address), null);
            Native.Cancel cancel = delegate { return stop.IsCancellationRequested ? 1 : 0; };
            IntPtr pad = Native.host_open(address.ToString(), 9000, cancel);
            GC.KeepAlive(cancel);
            if (pad == IntPtr.Zero)
            {
                stop.ThrowIfCancellationRequested();
                throw new InvalidOperationException(string.Format(Language.Get("No se pudo sincronizar con {0}. Abre WUP Stream y revisa su IP."), address));
            }
            try
            {
                stop.ThrowIfCancellationRequested();
                using (var client = new ViGEmClient())
                {
                    var controller = client.CreateXbox360Controller();
                    controller.AutoSubmitReport = false;
                    controller.FeedbackReceived += OnFeedback;
                    controller.Connect();
                    try { ReceiveControls(pad, controller, address); }
                    finally
                    {
                        controller.FeedbackReceived -= OnFeedback;
                        controller.Disconnect();
                    }
                }
            }
            finally { Native.host_close(pad); }
        }

        private void ReceiveControls(IntPtr pad, IXbox360Controller controller, IPAddress address)
        {
            var desktop = new DesktopStream(options.Monitor);
            using (var videoStop = CancellationTokenSource.CreateLinkedTokenSource(stop))
            {
                CancellationToken videoToken = videoStop.Token;
                Task video = options.Video ? Task.Factory.StartNew(() => desktop.Run(pad, videoToken),
                    CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default) : null;
                try
                {
                    using (var audio = new PadAudio(options))
                        Poll(pad, controller, address, desktop, audio, video);
                }
                finally
                {
                    desktop.ReleaseTouch();
                    Native.host_rumble(pad, 0);
                    videoStop.Cancel();
                    if (video != null) video.GetAwaiter().GetResult();
                }
            }
        }

        private void Poll(IntPtr pad, IXbox360Controller controller, IPAddress address, DesktopStream desktop,
            PadAudio audio, Task video)
        {
            var report = new byte[128];
            var aux = new int[11];
            int[] centers = null;
            int lastSequence = -1, lastBrightness = -1;
            bool motor = false;
            var clock = Stopwatch.StartNew();
            long repaintAt = 0;
            string connected = Language.Get("Conectado · ") + address;
            while (!stop.IsCancellationRequested)
            {
                if (video != null && video.IsFaulted) video.GetAwaiter().GetResult();
                if (Native.host_input(pad, report, aux) != 0)
                    throw new InvalidOperationException(Language.Get("Se perdió la conexión con el GamePad."));
                if (centers == null)
                {
                    centers = new int[4];
                    for (int i = 0; i < centers.Length; i++)
                    {
                        int raw = PadState.ReadAxis(report, i);
                        if (Math.Abs(raw) < 300) centers[i] = raw;
                    }
                }

                int sequence = report[0] | (report[1] << 8);
                if (sequence != lastSequence)
                {
                    lastSequence = sequence;
                    Submit(controller, new PadState(report, centers));
                }
                desktop.Touch(options.Video && options.Touch && aux[0] != 0, aux[1], aux[2]);

                int brightness = options.Brightness;
                if (brightness != lastBrightness)
                {
                    Native.host_light(pad, brightness);
                    lastBrightness = brightness;
                }
                bool vibrate = rumble || Stopwatch.GetTimestamp() < Interlocked.Read(ref options.VibrationUntil);
                if (vibrate != motor)
                {
                    Native.host_rumble(pad, vibrate ? 1 : 0);
                    motor = vibrate;
                }
                audio.Pump(pad, clock.ElapsedMilliseconds);
                if (clock.ElapsedMilliseconds >= repaintAt)
                {
                    var view = new PadState(report, centers)
                    {
                        MicLevel = audio.Level, AudioStatus = audio.Status, Backlight = aux[10], Volume = aux[9],
                        Accel = new[] { aux[3], aux[4], aux[5] }, Gyro = new[] { aux[6], aux[7], aux[8] }
                    };
                    publish(connected, view);
                    repaintAt = clock.ElapsedMilliseconds + 45;
                }
                Thread.Sleep(4);
            }
        }

        private IPAddress Discover()
        {
            using (var udp = new UdpClient(AddressFamily.InterNetwork))
            {
                udp.ExclusiveAddressUse = true;
                try { udp.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort)); }
                catch (SocketException ex)
                {
                    throw new InvalidOperationException(Language.Get("No se puede abrir UDP 50001. Puedes conectar con la IP del GamePad."), ex);
                }
                udp.Client.ReceiveTimeout = 100;
                var clock = Stopwatch.StartNew();
                var sender = new IPEndPoint(IPAddress.Any, 0);
                while (clock.ElapsedMilliseconds < 25000)
                {
                    stop.ThrowIfCancellationRequested();
                    byte[] beacon = Receive(udp, ref sender);
                    if (beacon != null && beacon.Length == 4 && beacon[0] == 'B'
                        && beacon[1] == 1 && sender.Port == PadPort)
                        return sender.Address;
                }
                throw new InvalidOperationException(Language.Get("No se encontró el GamePad. Prueba con la IP que muestra WUP Stream."));
            }
        }

        private void OnFeedback(object sender, Xbox360FeedbackReceivedEventArgs e)
        {
            rumble = e.LargeMotor != 0 || e.SmallMotor != 0;
        }

        private static void Submit(IXbox360Controller controller, PadState state)
        {
            controller.SetButtonsFull(state.Buttons);
            controller.SetSliderValue(Xbox360Slider.LeftTrigger, state.Held[6] ? (byte)255 : (byte)0);
            controller.SetSliderValue(Xbox360Slider.RightTrigger, state.Held[7] ? (byte)255 : (byte)0);
            controller.SetAxisValue(Xbox360Axis.LeftThumbX, state.Axes[0]);
            controller.SetAxisValue(Xbox360Axis.LeftThumbY, state.Axes[1]);
            controller.SetAxisValue(Xbox360Axis.RightThumbX, state.Axes[2]);
            controller.SetAxisValue(Xbox360Axis.RightThumbY, state.Axes[3]);
            controller.SubmitReport();
        }

        private static byte[] Receive(UdpClient udp, ref IPEndPoint sender)
        {
            try { return udp.Receive(ref sender); }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.TimedOut && ex.SocketErrorCode != SocketError.WouldBlock)
                    throw;
                return null;
            }
        }
    }
}
