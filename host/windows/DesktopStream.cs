// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WupPad
{
    internal sealed class DesktopStream
    {
        internal const int Width = 854;
        internal const int Height = 480;
        private static readonly Rectangle Frame = new Rectangle(0, 0, Width, Height);
        private readonly Rectangle desktop;
        private readonly Rectangle image;
        private bool held, beganOutside, wasTouched;
        private Point last;

        internal DesktopStream(Rectangle bounds)
        {
            desktop = bounds;
            double scale = Math.Min((double)Width / bounds.Width, (double)Height / bounds.Height);
            int width = Math.Max(1, (int)Math.Round(bounds.Width * scale));
            int height = Math.Max(1, (int)Math.Round(bounds.Height * scale));
            image = new Rectangle((Width - width) / 2, (Height - height) / 2, width, height);
        }

        internal void Run(IntPtr pad, CancellationToken stop, int captureFps)
        {
            Rectangle target = image;
            using (var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppRgb))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Black);
                var clock = Stopwatch.StartNew();
                double interval = captureFps < 60 ? 1000.0 / captureFps : 0;
                double next = 0;
                IntPtr dxgi = captureFps == 60 ? Native.host_desktop_open(MonitorFromPoint(
                    new Point(desktop.Left + desktop.Width / 2, desktop.Top + desktop.Height / 2), 0)) : IntPtr.Zero;
                try
                {
                    while (!stop.IsCancellationRequested)
                    {
                        if (interval > 0)
                        {
                            double current = clock.Elapsed.TotalMilliseconds;
                            if (next > current && stop.WaitHandle.WaitOne((int)Math.Ceiling(next - current))) return;
                            current = clock.Elapsed.TotalMilliseconds;
                            if (current > next + interval) next = current;
                            next += interval;
                        }
                        if (dxgi != IntPtr.Zero)
                        {
                            BitmapData capture = bitmap.LockBits(target, ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                            int read;
                            try { read = Native.host_desktop_read(dxgi, capture.Scan0, capture.Stride, target.Width, target.Height); }
                            finally { bitmap.UnlockBits(capture); }
                            if (read == 0) continue;
                            if (read == 2) DrawCursor(graphics, target);
                            if (read < 0)
                            {
                                Native.host_desktop_close(dxgi);
                                dxgi = IntPtr.Zero;
                            }
                        }
                        if (dxgi == IntPtr.Zero)
                        {
                            Capture(graphics, target);
                            DrawCursor(graphics, target);
                        }
                        BitmapData pixels = bitmap.LockBits(Frame, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                        int result;
                        try { result = Native.host_frame(pad, pixels.Scan0, pixels.Stride); }
                        finally { bitmap.UnlockBits(pixels); }
                        if (result != 0) return;
                    }
                }
                finally { Native.host_desktop_close(dxgi); }
            }
        }

        internal static void ShowControllerMode(IntPtr pad, CancellationToken stop)
        {
            if (stop.IsCancellationRequested) return;
            using (var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppRgb))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                using (var font = new Font("Segoe UI Semibold", 33, GraphicsUnit.Pixel))
                using (var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    graphics.Clear(Color.FromArgb(9, 19, 42));
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    graphics.DrawString(Language.Get("Modo mando"), font, Brushes.White, Frame, center);
                }
                BitmapData pixels = bitmap.LockBits(Frame, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                try { Native.host_frame(pad, pixels.Scan0, pixels.Stride); }
                finally { bitmap.UnlockBits(pixels); }
            }
        }

        private void Capture(Graphics graphics, Rectangle target)
        {
            IntPtr source = GetDC(IntPtr.Zero);
            if (source == IntPtr.Zero) throw new InvalidOperationException(Language.Get("Windows no permitió capturar el escritorio."));
            try
            {
                IntPtr dest = graphics.GetHdc();
                try
                {
                    SetStretchBltMode(dest, 4); // HALFTONE
                    SetBrushOrgEx(dest, 0, 0, IntPtr.Zero);
                    const uint copyDesktop = 0x00CC0020 | 0x40000000; // SRCCOPY | CAPTUREBLT
                    if (!StretchBlt(dest, target.X, target.Y, target.Width, target.Height, source,
                        desktop.X, desktop.Y, desktop.Width, desktop.Height, copyDesktop) &&
                        !StretchBlt(dest, target.X, target.Y, target.Width, target.Height, source,
                        desktop.X, desktop.Y, desktop.Width, desktop.Height, 0x00CC0020))
                        throw new InvalidOperationException(Language.Get("La captura del monitor se interrumpió."));
                }
                finally { graphics.ReleaseHdc(dest); }
            }
            finally { ReleaseDC(IntPtr.Zero, source); }
        }

        private void DrawCursor(Graphics graphics, Rectangle target)
        {
            var cursor = new CursorInfo { Size = Marshal.SizeOf(typeof(CursorInfo)) };
            if (GetCursorInfo(ref cursor) && (cursor.Flags & 1) != 0 && desktop.Contains(cursor.Position))
            {
                int x = target.X + (cursor.Position.X - desktop.X) * target.Width / desktop.Width;
                int y = target.Y + (cursor.Position.Y - desktop.Y) * target.Height / desktop.Height;
                int size = target.Width < image.Width ? 8 : 19;
                var clip = graphics.Save();
                graphics.SetClip(target);
                Cursors.Arrow.Draw(graphics, new Rectangle(x, y, size, size));
                graphics.Restore(clip);
            }
        }

        internal void Touch(bool touching, int x, int y)
        {
            if (!touching)
            {
                ReleaseTouch();
                return;
            }

            if (!wasTouched) beganOutside = !image.Contains(x, y);
            wasTouched = true;
            if (beganOutside) return;

            int px = Math.Max(0, Math.Min(image.Width - 1, x - image.X));
            int py = Math.Max(0, Math.Min(image.Height - 1, y - image.Y));
            var point = new Point(desktop.X + px * (desktop.Width - 1) / Math.Max(1, image.Width - 1),
                desktop.Y + py * (desktop.Height - 1) / Math.Max(1, image.Height - 1));
            if (!held || last != point)
            {
                if (!SetCursorPos(point.X, point.Y)) return;
                last = point;
            }
            if (!held) held = SendButton(0x0002); // left button down
        }

        internal void ReleaseTouch()
        {
            if (held) held = !SendButton(0x0004); // retry on next poll if Windows did not accept it
            wasTouched = false;
            beganOutside = false;
        }

        private static bool SendButton(uint flags)
        {
            var input = new MouseInput { Type = 0, Flags = flags };
            return SendInput(1, ref input, Marshal.SizeOf(typeof(MouseInput))) == 1;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CursorInfo
        {
            internal int Size, Flags;
            internal IntPtr Handle;
            internal Point Position;
        }

        // INPUT contains a pointer-aligned union; MOUSEINPUT supplies its largest member.
        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            internal uint Type;
            private readonly uint padding;
            private readonly int dx, dy;
            private readonly uint data;
            internal uint Flags;
            private readonly uint time;
            private readonly UIntPtr extra;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(Point point, uint flags);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CursorInfo cursor);
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint count, ref MouseInput input, int size);
        [DllImport("gdi32.dll")]
        private static extern int SetStretchBltMode(IntPtr dc, int mode);
        [DllImport("gdi32.dll")]
        private static extern bool SetBrushOrgEx(IntPtr dc, int x, int y, IntPtr old);
        [DllImport("gdi32.dll")]
        private static extern bool StretchBlt(IntPtr dest, int x, int y, int width, int height,
            IntPtr source, int sx, int sy, int sw, int sh, uint operation);
    }
}
