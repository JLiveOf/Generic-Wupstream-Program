// SPDX-License-Identifier: GPL-3.0-or-later
using System;
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

        internal void Run(IntPtr pad, CancellationToken stop)
        {
            using (var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppRgb))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Black);
                while (!stop.IsCancellationRequested)
                {
                    Capture(graphics);
                    BitmapData pixels = bitmap.LockBits(Frame, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                    int result;
                    try { result = Native.host_frame(pad, pixels.Scan0, pixels.Stride); }
                    finally { bitmap.UnlockBits(pixels); }
                    if (result != 0) return;
                }
            }
        }

        private void Capture(Graphics graphics)
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
                    if (!StretchBlt(dest, image.X, image.Y, image.Width, image.Height, source,
                        desktop.X, desktop.Y, desktop.Width, desktop.Height, copyDesktop) &&
                        !StretchBlt(dest, image.X, image.Y, image.Width, image.Height, source,
                        desktop.X, desktop.Y, desktop.Width, desktop.Height, 0x00CC0020))
                        throw new InvalidOperationException(Language.Get("La captura del monitor se interrumpió."));
                }
                finally { graphics.ReleaseHdc(dest); }
            }
            finally { ReleaseDC(IntPtr.Zero, source); }

            var cursor = new CursorInfo { Size = Marshal.SizeOf(typeof(CursorInfo)) };
            if (GetCursorInfo(ref cursor) && (cursor.Flags & 1) != 0 && desktop.Contains(cursor.Position))
            {
                int x = image.X + (cursor.Position.X - desktop.X) * image.Width / desktop.Width;
                int y = image.Y + (cursor.Position.Y - desktop.Y) * image.Height / desktop.Height;
                var clip = graphics.Save();
                graphics.SetClip(image);
                Cursors.Arrow.Draw(graphics, new Rectangle(x, y, 19, 19));
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
