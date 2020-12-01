using UserSettingsStruct;
using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using ScreenShotWindows.Utils;
using System.Linq;
using System.Windows.Interop;

namespace ScreenShotWindows
{
	public enum WindowsCaptureScreenTarget 
	{
		MainScreen,ActiveScreen,AllScreens
	}
	public enum WindowsCaptureMode 
	{ 
		Frame,Continuous
	}
	/// <summary>
	/// A wrapper class for ScreenShotWindows, called from outside. Snapped Event is provided as a callback when screenshots are completed.
	/// </summary>
	public class ScreenShot
	{
		private List<ScreensShotWindows> _allWindows = new List<ScreensShotWindows>();
		private int windowsCreatedCount = 0;
		public void Start(WindowsCaptureScreenTarget screenTarget, WindowsCaptureMode captureMode, UserSettingsForScreenShotWindows userSettings, bool isSavingToLocal = true)
		{
			LogSystemShared.LogWriter.WriteLine(Enum.GetName(typeof(WindowsCaptureScreenTarget), screenTarget), "Capture Target");
			LogSystemShared.LogWriter.WriteLine(Enum.GetName(typeof(WindowsCaptureMode), captureMode), "Capture Mode");
			if(captureMode == WindowsCaptureMode.Frame)
			{
				if(screenTarget == WindowsCaptureScreenTarget.MainScreen)
				{
					windowsCreatedCount++;
					var mainScreen = System.Windows.Forms.Screen.PrimaryScreen;
					new ScreensShotWindows(mainScreen, this, userSettings).Show();
				}
				else if(screenTarget == WindowsCaptureScreenTarget.ActiveScreen)
				{
					windowsCreatedCount++;
					InteropMethods.GetCursorPos_(out InteropStructs.POINT pt);
					var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(pt.X, pt.Y));

					new ScreensShotWindows(screen, this, userSettings).Show();
				}
				else if(screenTarget == WindowsCaptureScreenTarget.AllScreens)
				{
					TargetAreaSelected += OneWindowTargetAreaSelected;
					foreach(var screen in System.Windows.Forms.Screen.AllScreens)
					{
						windowsCreatedCount++;
						var ssw = new ScreensShotWindows(screen, this, userSettings);
						_allWindows.Add(ssw);
						ssw.Show();
					}
				}
			}
			else if(captureMode == WindowsCaptureMode.Continuous)
			{
				if(screenTarget == WindowsCaptureScreenTarget.MainScreen)
				{
					windowsCreatedCount++;
					var mainScreen = System.Windows.Forms.Screen.PrimaryScreen;
					new WhiteDipWindow(mainScreen, this, userSettings, isSavingToLocal).Show();
				}
				else if(screenTarget == WindowsCaptureScreenTarget.ActiveScreen)
				{
					windowsCreatedCount++;
					InteropMethods.GetCursorPos_(out InteropStructs.POINT pt);
					var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(pt.X, pt.Y));

					new WhiteDipWindow(screen, this, userSettings, isSavingToLocal).Show();
				}
				else if(screenTarget == WindowsCaptureScreenTarget.AllScreens)
				{
					foreach(var screen in System.Windows.Forms.Screen.AllScreens)
					{
						windowsCreatedCount++;
						new WhiteDipWindow(screen, this,userSettings, isSavingToLocal).Show();
					}
				}
			}
		}

		#region events
		/// <summary>
		/// This is called whenever whitedip window is not saving to local 
		/// </summary>
		public static event EventHandler<(System.Windows.Forms.Screen screen, BitmapSource image)> Snapped;
		internal void OnSnapped(System.Windows.Forms.Screen screen, BitmapSource source) => Snapped?.Invoke(this, (screen,source));

		public static event EventHandler<EventArgs> AllClosed;
		internal void OnClose()
		{
			windowsCreatedCount--;
			if(windowsCreatedCount == 0)
			{
				AllClosed?.Invoke(this, new EventArgs());
				LogSystemShared.LogWriter.WriteLine("All closed event called");
			}
		}


		internal static event EventHandler<ScreensShotWindows> TargetAreaSelected;
		internal void OnTargetAreaSelected(ScreensShotWindows win) => TargetAreaSelected?.Invoke(this, win);
		private void OneWindowTargetAreaSelected(object sender, ScreensShotWindows args)
		{
			foreach(var other in _allWindows)
			{
				if(other != args)
				{
					other.Close();
				}
			}
			_allWindows.Clear();
		}
		#endregion

		private static readonly Guid BmpGuid = new Guid("{b96b3cab-0728-11d3-9d7b-0000f81ef32e}"); // do not change a bit
		internal static BitmapImage GetSnapShot(Window callerWindow, InteropStructs.RECT rect)
		{
			try
			{
				IntPtr windowHandler = new WindowInteropHelper(callerWindow).Handle;
				var hdcSrc = InteropMethods.GetWindowDC_(windowHandler);
				var hdcDest = InteropMethods.CreateCompatibleDC_(hdcSrc);

				//InteropMethods.GetWindowRect_(windowHandler, out _desktopWindowRect);
				LogSystemShared.LogWriter.WriteLine($"Captured window size is: {(int)rect.Width:0}*{(int)rect.Height}");
				//InteropMethods.GetWindowInfo_(windowHandler, out InteropStructs.WINDOWINFO windowInfo);
				//_desktopWindowRect = windowInfo.rcWindow;
				//LogWriter.WriteLine($"Captured window size from window info call is: {_desktopWindowRect.Width}*{_desktopWindowRect.Height}");
				//LogWriter.WriteLine($"Captured window (client) size from window info call is: {windowInfo.rcClient.Width}*{windowInfo.rcClient.Height}");

				var hbitmap = InteropMethods.CreateCompatibleBitmap_(hdcSrc, (int)rect.Width, (int)rect.Height);


				var hOld = InteropMethods.SelectObject_(hdcDest, hbitmap);
				InteropMethods.BitBlt_(hdcDest, 0, 0, (int)rect.Width, (int)rect.Height, hdcSrc, 0, 0, InteropConstants.SRCCOPY);
				InteropMethods.SelectObject_(hdcDest, hOld);
				InteropMethods.DeleteDC_(hdcDest);
				InteropMethods.ReleaseDC_(windowHandler, hdcSrc);

				InteropMethods.Gdip.GdipCreateBitmapFromHBITMAP_(new HandleRef(null, hbitmap), new HandleRef(null, IntPtr.Zero), out var bitmap).GdipExceptionHandler();

				using var ms = new MemoryStream();
				InteropMethods.Gdip.GdipGetImageEncodersSize_(out var numEncoders, out var size).GdipExceptionHandler();

				var memory = Marshal.AllocHGlobal(size);
				InteropMethods.Gdip.GdipGetImageEncoders_(numEncoders, size, memory).GdipExceptionHandler();

				var codecInfo = ImageCodecInfo.ConvertFromMemory(memory, numEncoders).FirstOrDefault(item => item.FormatID.Equals(BmpGuid));
				if(codecInfo == null) throw new Exception("ImageCodecInfo is null");

				var encoderParamsMemory = IntPtr.Zero;

				var g = codecInfo.Clsid;
				InteropMethods.Gdip.GdipSaveImageToStream_(new HandleRef(callerWindow, bitmap),
					new InteropStructs.ComStreamFromDataStream(ms), ref g,
					new HandleRef(null, encoderParamsMemory)).GdipExceptionHandler();
				if(encoderParamsMemory != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(encoderParamsMemory);
				}

				Marshal.FreeHGlobal(memory);

				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.StreamSource = ms;
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				return bitmapImage;
			}
			catch(Exception e)
			{
				LogSystemShared.LogWriter.WriteLine(e.Message, "Fatal error");
				Application.Current.Shutdown();
				return null;
			}
		}
	}
}
