using UserSettingsStruct;
using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

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
		public void Start(WindowsCaptureScreenTarget screenTarget, WindowsCaptureMode captureMode, UserSettingsForScreenShotWindows userSettings)
		{
			if(captureMode == WindowsCaptureMode.Frame)
			{
				if(screenTarget == WindowsCaptureScreenTarget.MainScreen)
					new ScreensShotWindows(this, screenTarget, captureMode, userSettings).Show();
				else if(screenTarget == WindowsCaptureScreenTarget.ActiveScreen)
				{
					InteropMethods.GetCursorPos_(out InteropStructs.POINT pt);
					var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(pt.X, pt.Y));
					new ScreensShotWindows(screen, this, screenTarget, captureMode, userSettings).Show();
				}
				else if(screenTarget == WindowsCaptureScreenTarget.AllScreens)
				{
					TargetAreaSelected += OneWindowTargetAreaSelected;
					foreach(var screen in System.Windows.Forms.Screen.AllScreens)
					{
						var ssw = new ScreensShotWindows(screen, this, screenTarget, captureMode, userSettings);
						_allWindows.Add(ssw);
						ssw.Show();
					}
				}
			}
		}

		private void OneWindowTargetAreaSelected(object sender, FunctionEventArgs<ScreensShotWindows> args)
		{
			var ssw = args.Info;
			foreach(var other in _allWindows)
			{
				if(other != ssw)
				{
					other.Close();
				}
			}
		}

		public static event EventHandler<FunctionEventArgs<ImageSource[]>> Snapped;
		internal void OnSnapped(ImageSource[] source) => Snapped?.Invoke(this, new FunctionEventArgs<ImageSource[]>(source));
		public static event EventHandler<EventArgs> Closed;
		internal void OnClose() => Closed?.Invoke(this, new EventArgs());
		internal static event EventHandler<FunctionEventArgs<ScreensShotWindows>> TargetAreaSelected;
		internal void OnTargetAreaSelected(ScreensShotWindows win) => TargetAreaSelected?.Invoke(this, new FunctionEventArgs<ScreensShotWindows>(win));
	}

	public class FunctionEventArgs<T>: RoutedEventArgs
	{
		public T Info { get; set; }
		public FunctionEventArgs(T info) => Info = info;
		public FunctionEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source) { }
	}
}
