using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenShotWindows.Utils.Interop
{
	internal static class InteropErrorHandlerExt
	{
		private static int lastError = 0;
		internal static TResult HandleError<TResult>(this TResult result, [CallerMemberName] string memberName="", [CallerLineNumber] int lineNumber = 0)
		{
			int currentError = Marshal.GetLastWin32Error();
			if(lastError != currentError)
			{
				LogSystemShared.LogWriter.WriteLine($"Win32 error occurred: No.{currentError}.", memberName: memberName, sourceLineNumber: lineNumber);
				lastError = currentError;
			}
			return result;
		}
	}
	internal class InteropMethods
	{
		/// <summary>
		/// Retrieves a handle to the desktop window. The desktop window covers the entire screen. The desktop window is the area on top of which other windows are painted.
		/// </summary>
		/// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetDesktopWindow();
		public static IntPtr GetDesktopWindow_()=>GetDesktopWindow().HandleError();

		/// <summary>
		/// The GetWindowDC function retrieves the device context (DC) for the entire window, including title bar, menus, and scroll bars.
		/// </summary>
		/// <param name="window"></param>
		/// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetWindowDC(IntPtr window);
		public static IntPtr GetWindowDC_(IntPtr window) => GetWindowDC(window).HandleError();

		/// <summary>
		/// Installs an application-defined hook procedure into a hook chain.
		/// SetLastError: true to indicate that the callee will call SetLastError; otherwise, false. The default is false. The runtime marshaler calls GetLastError and caches the value returned to prevent it from being overwritten by other API calls.You can retrieve the error code by calling Marshal.GetLastWin32Error().
		/// </summary>
		/// <param name="idHook"></param>
		/// <param name="lpfn">A pointer to the hook procedure.</param>
		/// <param name="hMod"></param>
		/// <param name="dwThreadId">The identifier of the thread with which the hook procedure is to be associated. For desktop apps, if this parameter is zero</param>
		/// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, InteropStructs.HookProc lpfn, IntPtr hMod, uint dwThreadId);
		public static IntPtr SetWindowsHookEx_(int idHook, InteropStructs.HookProc lpfn, IntPtr hMod, uint dwThreadId) => SetWindowsHookEx(idHook, lpfn, hMod, dwThreadId).HandleError();
		/// <summary>
		/// Removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.
		/// </summary>
		/// <param name="hhk"></param>
		/// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);
		public static bool UnhookWindowsHookEx_(IntPtr hhk)=>UnhookWindowsHookEx(hhk).HandleError();

		/// <summary>
		/// Retrieves a module handle for the specified module. The module must have been loaded by the calling process.
		/// </summary>
		/// <param name="lpModuleName"></param>
		/// <returns></returns>
		[DllImport(InteropConstants.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
		public static IntPtr GetModuleHandle_(string lpModuleName) => GetModuleHandle(lpModuleName).HandleError();

		/// <summary>
		/// Passes the hook information to the next hook procedure in the current hook chain. A hook procedure can call this function either before or after processing the hook information.
		/// </summary>
		/// <param name="hhk"></param>
		/// <param name="nCode"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
		public static IntPtr CallNextHookEx_(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam)=>CallNextHookEx(hhk,nCode,wParam,lParam).HandleError();


		#region multiple screens interface
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool EnumWindows(InteropStructs.EnumWindowsProc enumProc, IntPtr lParam);
		#endregion
	}
}
