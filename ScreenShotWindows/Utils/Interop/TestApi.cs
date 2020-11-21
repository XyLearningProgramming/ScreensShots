using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace ScreenShotWindows.Utils.Interop
{
	public class TestApi
	{
		public static void StopHooks(Action<string> callback)
		{
			MouseHook.Stop();
			MouseHook.StatusChanged -= (o, e) => 
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("Mouse event: ");
				sb.Append(Enum.GetName(typeof(InteropStructs.MouseHookMessageType), e.MessageType));
				sb.Append("; Point: ");
				sb.Append($"({e.Point.X},{e.Point.Y})");
				sb.Append(Environment.NewLine);
				callback(sb.ToString());
			};
		}
		public static void StartHooks(Action<string> callback)
		{
			MouseHook.Start();
			MouseHook.StatusChanged += (o, e) =>
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("Mouse event: ");
				sb.Append(Enum.GetName(typeof(InteropStructs.MouseHookMessageType), e.MessageType));
				sb.Append("; Point: ");
				sb.Append($"({e.Point.X},{e.Point.Y})");
				sb.Append(Environment.NewLine);
				callback(sb.ToString());
			};
		}

		public static string EnumMonitor()
		{
			StringBuilder sb = new StringBuilder();
			InteropStructs.MonitorEnumProc callback = (IntPtr hDesktop, IntPtr hdc, ref InteropStructs.RECT prect, int d)=>
				{
					sb.Append($"{prect.Width}*{prect.Height} with starting point {prect.Top}*{prect.Left}");
					sb.Append(Environment.NewLine);
					return true;
				};
			InteropMethods.EnumDisplayMonitors_(IntPtr.Zero,IntPtr.Zero,callback,0);

			sb.Append($"virtual screen width {SystemParameters.VirtualScreenWidth}*{SystemParameters.VirtualScreenHeight}");
			sb.Append(Environment.NewLine);

			foreach(var screen in System.Windows.Forms.Screen.AllScreens)
			{
				uint x, y;
				screen.GetDpi(DpiType.Effective, out x, out y);
				sb.Append(screen.DeviceName + " - dpiX=" + x + ", dpiY=" + y);
				screen.GetDpi(DpiType.Raw, out x, out y);
				sb.Append(screen.DeviceName + " - dpiX=" + x + ", dpiY=" + y);
				sb.Append(Environment.NewLine);
				screen.GetDpi(DpiType.Angular, out x, out y);
				sb.Append(screen.DeviceName + " - dpiX=" + x + ", dpiY=" + y);
				sb.Append(Environment.NewLine);
			}
			return sb.ToString();
		}
	}

	public static class ScreenExtensions
	{
		public static void GetDpi(this System.Windows.Forms.Screen screen, DpiType dpiType, out uint dpiX, out uint dpiY)
		{
			var pnt = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
			var mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
			GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
		}

		//https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
		[DllImport("User32.dll")]
		private static extern IntPtr MonitorFromPoint([In] System.Drawing.Point pt, [In] uint dwFlags);

		//https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
		[DllImport("Shcore.dll")]
		private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);
	}

	//https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx
	public enum DpiType
	{
		Effective = 0,
		Angular = 1,
		Raw = 2,
	}
}
