using System;
using System.Collections.Generic;
using System.Text;

namespace ScreenShotWindows
{
	internal class ScreensShotWindows: System.Windows.Window
	{
		#region fields
		ScreenShot _screenShot;
		WindowsCaptureScreenTarget _screenTarget;
		WindowsCaptureMode _captureMode;
		#endregion

		internal ScreensShotWindows(ScreenShot screenShot, WindowsCaptureScreenTarget screenTarget, WindowsCaptureMode captureMode)
		{
			_screenShot = screenShot;
			_screenTarget = screenTarget;
			_captureMode = captureMode;
		}
	}
}
