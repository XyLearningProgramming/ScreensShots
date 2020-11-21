using UserSettingsStruct;
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
		public void Start(WindowsCaptureScreenTarget screenTarget, WindowsCaptureMode captureMode, UserSettingsForScreenShotWindows userSettings)
		{
			if(screenTarget != WindowsCaptureScreenTarget.AllScreens)
				new ScreensShotWindows(this, screenTarget, captureMode, userSettings).Show();
			else // TODO
			{ 
			
			}
		}

		public static event EventHandler<FunctionEventArgs<ImageSource[]>> Snapped;
		internal void OnSnapped(ImageSource[] source) => Snapped?.Invoke(this, new FunctionEventArgs<ImageSource[]>(source));
	}

	public class FunctionEventArgs<T>: RoutedEventArgs
	{
		public T Info { get; set; }
		public FunctionEventArgs(T info) => Info = info;
		public FunctionEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source) { }
	}
}
