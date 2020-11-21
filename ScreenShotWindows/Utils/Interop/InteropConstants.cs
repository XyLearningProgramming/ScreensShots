using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ScreenShotWindows.Utils.Interop
{
    internal static class InteropConstants
    {
        #region external dlls
        internal const string User32 = "user32.dll";
        internal const string Kernel32 = "kernel32.dll";
        internal const string Gdi32 = "gdi32.dll"; // graphics device interface that perform primitive drawing functions 
        internal const string Gdiplus = "gdiplus.dll";
        internal const string NTdll = "ntdll.dll";
        #endregion

        internal const int SRCCOPY = 0x00CC0020;
    }
}
