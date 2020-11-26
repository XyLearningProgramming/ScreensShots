using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using Keys = System.Windows.Forms.Keys;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ScreenShotApp.Utils
{
	public class KeyboardHook
	{
		private const HookType _hookType = HookType.WH_KEYBOARD_LL;
		private static int _hookCount = 0;
		private static IntPtr _hookHandle = IntPtr.Zero;
		private static List<(Keys modifier, Keys mainkey)> shortcuts = new List<(Keys modifier, Keys mainkey)>();
		static KeyboardHook()
		{
			_hookCount = 0;
			_hookHandle = IntPtr.Zero;
		}
		public static void StartHook(Keys modifier, Keys mainkey, [CallerMemberName] string membername = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int lineNum = 0)
		{
			if(_hookCount == 0 && _hookHandle == IntPtr.Zero)
			{
				using Process currentProcess = Process.GetCurrentProcess();
				using ProcessModule currentPM = currentProcess.MainModule;
				if(currentPM != null)
				{
					_hookHandle = SetWindowsHookEx_((int)_hookType, HookCallback, GetModuleHandle_(currentPM.ModuleName), 0);
					LogSystemShared.LogWriter.WriteLine("Keyboard hooked successfully.", memberName: membername, sourceFilePath: callerFilePath, sourceLineNumber: lineNum);
				}
				else
				{
					LogSystemShared.LogWriter.WriteLine("Trying to hook keyboard but unable to find current module.", title: "Unable to find module.");
					_hookHandle = IntPtr.Zero;
				}
			}
			_hookCount++;
			shortcuts.Add((modifier, mainkey));
		}
		public static void StopHook(Keys modifier, Keys mainkey)
		{
			var tp = (modifier, mainkey);
			if(!shortcuts.Contains(tp)) return;
			_hookCount--;
			shortcuts.Remove(tp);

			if(_hookCount == 0 && _hookHandle!=IntPtr.Zero)
			{
				UnhookWindowsHookEx_(_hookHandle);
				_hookHandle = IntPtr.Zero;
				LogSystemShared.LogWriter.WriteLine("Unhook keyboard successfully.");
			}
		}

		private static IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
		{
			if(code < 0)
				return CallNextHookEx_(_hookHandle, code, wParam, lParam);

			//// KeyUp event
			//if((lParam.flags & 0x80) != 0 && this.KeyUp != null)
			//	this.KeyUp(this, new HookEventArgs(lParam.vkCode));

			// KeyDown event
			KBDLLHOOKSTRUCT keyboardStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

			if((keyboardStruct.flags & 0x80) == 0)
			{
				Keys pressedMain = (Keys)keyboardStruct.vkCode;
				var possibleComb = shortcuts.Where(s => s.mainkey == pressedMain);
				if(possibleComb.Count() != 0)
				{
					foreach(var (modifier,_) in possibleComb)
					{
						if(System.Windows.Forms.Control.ModifierKeys.Equals(modifier))
						{
							OnKeyboardPressed(modifier, pressedMain);
							LogSystemShared.LogWriter.WriteLine($"Get key pressed {modifier}, {pressedMain}");
							break;
						}
					}
				}
			}

			return CallNextHookEx_(_hookHandle, code, wParam, lParam);
		}

		#region event
		public class KeyBoardEventArgs 
		{
			public System.Windows.Forms.Keys Modifier;
			public System.Windows.Forms.Keys Mainkey;
		}
		public static event EventHandler<KeyBoardEventArgs> KeyboardPressed;
		private static void OnKeyboardPressed(Keys Modifier_, Keys Mainkey_) => KeyboardPressed?.Invoke(null, new KeyBoardEventArgs() { Modifier = Modifier_, Mainkey = Mainkey_ });
		#endregion

		#region Interop
		private enum HookType : int
		{
			WH_JOURNALRECORD = 0,
			WH_JOURNALPLAYBACK = 1,
			WH_KEYBOARD = 2,
			WH_GETMESSAGE = 3,
			WH_CALLWNDPROC = 4,
			WH_CBT = 5,
			WH_SYSMSGFILTER = 6,
			WH_MOUSE = 7,
			WH_HARDWARE = 8,
			WH_DEBUG = 9,
			WH_SHELL = 10,
			WH_FOREGROUNDIDLE = 11,
			WH_CALLWNDPROCRET = 12,
			WH_KEYBOARD_LL = 13,
			WH_MOUSE_LL = 14
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct KBDLLHOOKSTRUCT
		{
			public UInt32 vkCode;
			public UInt32 scanCode;
			public UInt32 flags;
			public UInt32 time;
			public IntPtr extraInfo;
		}

		private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int code, HookProc func, IntPtr instance, int threadID);
		private static IntPtr SetWindowsHookEx_(int code, HookProc func, IntPtr instance, int threadID) => SetWindowsHookEx(code, func, instance, threadID).HandleError();

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr UnhookWindowsHookEx(IntPtr hook);
		private static IntPtr UnhookWindowsHookEx_(IntPtr hook) => UnhookWindowsHookEx(hook).HandleError();

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
		private static IntPtr CallNextHookEx_(IntPtr hook, int code, IntPtr wParam, IntPtr lParam)=>CallNextHookEx(hook,code,wParam,lParam).HandleError();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
		private static IntPtr GetModuleHandle_(string lpModuleName)=>GetModuleHandle(lpModuleName).HandleError();
		#endregion
	}

	internal static class InteropErrorHandlerExt
	{
		private static int lastError = 0;
		internal static TResult HandleError<TResult>(this TResult result, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
		{
			int currentError = Marshal.GetLastWin32Error();
			if(lastError != currentError)
			{
				LogSystemShared.LogWriter.WriteLine($"Win32 error occurred: No.{currentError} with result type {nameof(TResult)}.", memberName: memberName, sourceLineNumber: lineNumber);
				lastError = currentError;
			}
			return result;
		}
	}
}
