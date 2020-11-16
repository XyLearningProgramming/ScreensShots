using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenShotWindows.Utils.Interop
{
	internal class InteropStructs
	{
		[StructLayout(LayoutKind.Sequential)]
		internal struct POINT 
		{
			public int X;
			public int Y;
			public POINT(int x, int y)
			{
				X = x; Y = y;
			}
		}

		/// <summary>
		/// An application-defined or library-defined callback function used with the SetWindowsHookEx function
		/// wParam: Specifies whether the message is sent by the current process. If the message is sent by the current process, it is nonzero; otherwise, it is NULL.
		/// lParam: A pointer to a CWPRETSTRUCT structure that contains details about the message.
		/// </summary>
		/// <param name="code">A code the hook procedure uses to determine how to process the message.</param>
		/// <param name="wParam">The virtual-key code of the key that generated the keystroke message.</param>
		/// <param name="lParam">The repeat count, scan code, extended-key flag, context code, previous key-state flag, and transition-state flag.</param>
		/// <returns></returns>
		internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
		internal delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		// Delegate to filter which windows to include 
		internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		internal enum HookType
		{
			MOUSE_LL,
			KEYBOARD_LL,
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct MOUSEHOOKSTRUCT
		{
			public Interop.InteropStructs.POINT pt;
			public IntPtr hwnd; // A handle to the window that processed the message specified by the message value.
			public uint wHitTestCode;
			public IntPtr dwExtraInfo;
		}

		// dark magic linked to Interop.InteropStructs.MOUSEHOOKSTRUCT. Do not change a bit
		internal enum MouseHookMessageType
		{
			LeftButtonDown = 0x0201, // 513
			LeftButtonUp = 0x0202,
			MouseMove = 0x0200,
			MouseWheel = 0x020A,
			RightButtonDown = 0x0204,
			RightButtonUp = 0x0205
		}
	}
}
