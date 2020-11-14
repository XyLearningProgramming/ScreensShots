using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenShotWindows.Utils.Interop
{
	internal class InteropMethods
	{
		[DllImport(InteropConstants.User32)]
		internal static extern IntPtr GetDesktopWindow();
	}
}
