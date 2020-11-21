using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenShotWindows.Utils
{
	/// <summary>
	/// Get global mouse position instead of that in the current application. In short, it hooks our event to the mouse hook chain
	/// </summary>
	internal class MouseHook
	{
        public static event EventHandler<MouseHookEventArgs> StatusChanged;
        private static IntPtr hookId= IntPtr.Zero;
        private static readonly Interop.InteropStructs.HookProc proc = HookCallback;
        private static int count; // count the times of trying to start hook, if reaches 0 when stop, then really stop the hook

        public static void Start()
		{
			if(hookId == IntPtr.Zero)
			{
                hookId = SetHook(proc);
			}
			else
			{
                count++;
			}
		}

        public static void Stop()
		{
            count--;
			if(count <= 0)
			{
                Interop.InteropMethods.UnhookWindowsHookEx_(hookId);
                LogSystemShared.LogWriter.WriteLine("Mouse unhooked successfully.");
                hookId = IntPtr.Zero;
			}
		}

        /// <summary>
        /// hook the mouse. Since the user32 method requires more info, so I wrap that here.
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        private static IntPtr SetHook(Interop.InteropStructs.HookProc proc)
		{
            using Process currentProcess = Process.GetCurrentProcess();
            using ProcessModule currentPM = currentProcess.MainModule;
			if(currentPM != null)
			{
                LogSystemShared.LogWriter.WriteLine("Mouse hooked successfully.");
                return Interop.InteropMethods.SetWindowsHookEx_((int)Interop.InteropStructs.HookType.MOUSE_LL, proc, Interop.InteropMethods.GetModuleHandle_(currentPM.ModuleName), 0);
			}
            LogSystemShared.LogWriter.WriteLine("Trying to hook mouse but unable to find current module.", title: "Unable to find module.");
            return IntPtr.Zero;
		}

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
            if(nCode < 0) return Interop.InteropMethods.CallNextHookEx_(hookId, nCode, wParam, lParam); // pass. Not my business
            Interop.InteropStructs.MOUSEHOOKSTRUCT hookStruct = (Interop.InteropStructs.MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Interop.InteropStructs.MOUSEHOOKSTRUCT));
            StatusChanged?.Invoke(null, new MouseHookEventArgs()
            {
                MessageType = (Interop.InteropStructs.MouseHookMessageType)wParam,
                Point = new Interop.InteropStructs.POINT(hookStruct.pt.X, hookStruct.pt.Y)
            });
            return Interop.InteropMethods.CallNextHookEx_(hookId, nCode, wParam, lParam);
        }

    }

    internal class MouseHookEventArgs : EventArgs
    {
        public Interop.InteropStructs.MouseHookMessageType MessageType { get; set; }

        public Interop.InteropStructs.POINT Point { get; set; }
        //TODO parse window handler in wParam? IntPtr
    }
}
