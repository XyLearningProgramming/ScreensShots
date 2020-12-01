using ScreenShotApp.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ScreenShotApp
{
	public interface IMyWindows
	{
		void TryRestoreWindowState();
		void MinimizeWindowState();
	}
	public class WindowBase : Window, IMyWindows
	{
		private WindowState _lastWindowState = WindowState.Normal;

		public void MinimizeWindowState()
		{
			_lastWindowState = this.WindowState;
			this.WindowState = WindowState.Minimized;
		}

		public void TryRestoreWindowState()
		{
			// not restoring to full scale when it's continuous mode
			if(UserSettingsManager.Instance.UserCaptureMode != ScreenShotWindows.WindowsCaptureMode.Continuous)
				this.WindowState = _lastWindowState;
		}

	}

}
