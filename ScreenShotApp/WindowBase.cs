using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ScreenShotApp
{
	public interface IMyWindows
	{
		void Normalize();
		void Minimize();
	}
	public class WindowBase : Window, IMyWindows
	{
		//private Size _idealSize = new Size();

		public void Minimize()
		{
			//this.Opacity = 0;
			//_idealSize = this.RenderSize;
			//this.RenderSize = new Size(0, 0);
			//this.Visibility = Visibility.Collapsed;
			this.WindowState = WindowState.Minimized;
		}

		public void Normalize()
		{
			//this.Opacity = 1;
			//this.Visibility = Visibility.Visible;
			this.WindowState = WindowState.Normal;
			//this.RenderSize = _idealSize;
		}


	}

}
