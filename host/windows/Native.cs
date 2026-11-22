// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Runtime.InteropServices;

namespace WupPad
{
    internal static class Native
    {
        private const string Library = "WupStreamNative.dll";
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int Cancel(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr host_open(string ip, int timeout, Cancel cancel);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr host_open_profile(string ip, int timeout, Cancel cancel, int lightVideo);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr host_open_settings(string ip, int timeout, Cancel cancel, int lightVideo, int maxKbps);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void host_close(IntPtr pad);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void host_interrupt(IntPtr pad);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int host_input(IntPtr pad, [Out] byte[] report, [Out] int[] extra);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int host_frame(IntPtr pad, IntPtr pixels, int stride);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr host_desktop_open(IntPtr monitor);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int host_desktop_read(IntPtr capture, IntPtr pixels, int stride, int width, int height);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void host_desktop_close(IntPtr capture);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int host_audio(IntPtr pad, short[] samples, int frames);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int host_mic(IntPtr pad, [Out] short[] samples, int count);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void host_rumble(IntPtr pad, int on);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void host_light(IntPtr pad, int level);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr audio_open(int cable, out int error);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void audio_close(IntPtr stream);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int audio_read(IntPtr stream, [Out] short[] samples, int frames);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int audio_write_mono(IntPtr stream, short[] samples, int frames);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int audio_same_device(IntPtr a, IntPtr b);
    }
}
