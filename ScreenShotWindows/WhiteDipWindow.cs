using LogSystemShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using UserSettingsStruct;

namespace ScreenShotWindows
{
	/// <summary>
	/// Snapshot, quickly dip to white, duration controlled by user
	/// </summary>
	[TemplatePart(Name = ElementInkCanvas, Type = typeof(InkCanvas))]
	[TemplatePart(Name = ElementShadowCanvas, Type = typeof(InkCanvas))]
	internal class WhiteDipWindow: Window
	{
		private const string ElementInkCanvas = "PART_InkCanvas";
		private const string ElementShadowCanvas = "PART_ShadowCanvas";
		private const double AnimationSkipThreshold = 0.1d;
		private bool _isInstantClose = false;
		private bool _isSavingToLocal = true;
		private UserSettingsForScreenShotWindows _userSettings;
		private System.Windows.Forms.Screen _currentScreen;
		private ScreenShot _screenShot;
		private BitmapImage _cachedSnapShot;

		internal InkCanvas InkCanvas { get; set; }
		internal InkCanvas ShadowCanvas { get; set; }

		internal WhiteDipWindow(System.Windows.Forms.Screen screen, ScreenShot screenShot, UserSettingsForScreenShotWindows userSettings, bool isSavingToLocal = true)
		{
			this._currentScreen = screen;
			this._screenShot = screenShot;
			this._isInstantClose = userSettings.WhiteDipAnimDuration < AnimationSkipThreshold || !userSettings.IsShowingWhiteDip;
			this._userSettings = userSettings;
			this._isSavingToLocal = isSavingToLocal;

			this.WindowStartupLocation = WindowStartupLocation.Manual;
			this.Left = screen.Bounds.Left;
			this.Top = screen.Bounds.Top;
			var hw = ScreenResolutionInferrer.GetInferredResolution(screen);
			this.Width = hw.width;
			this.Height = hw.height;
			
			Style = Application.Current.TryFindResource("WhiteDipWindowStyle") as Style;
			Debug.Assert(Style != null);

			this.Loaded += OnWhiteDipWindowLoaded;
			this.Closed += OnWhiteDipWindowClosed;

			LogWriter.WriteLine($"Trying to start white dip window at {this.Left}, {this.Top}, width {this.Width}, height {this.Height}");

		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			InkCanvas = GetTemplateChild(ElementInkCanvas) as InkCanvas;
			ShadowCanvas = GetTemplateChild(ElementShadowCanvas) as InkCanvas;
		}

		private void OnWhiteDipWindowLoaded(object sender, EventArgs args)
		{
			_cachedSnapShot = ScreenShot.GetSnapShot(this, new Utils.Interop.InteropStructs.RECT(Convert.ToInt32(this.Left), Convert.ToInt32(this.Top), Convert.ToInt32(this.Width + this.Left), Convert.ToInt32(this.Height + this.Top)));

			if(_isInstantClose)
			{
				// return result immediately, no dip white because it's too short
				this.Close();
				return;
			}

			// performing animation
			Image image = new Image() { Source = _cachedSnapShot, Width = _currentScreen.Bounds.Width, Height = _currentScreen.Bounds.Height };
			RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.Fant);
			LogWriter.WriteLine($"Reflected image size: width {(int)image.Source.Width}, height {(int)image.Source.Height}");
			InkCanvas.Children.Add(image); // canvas is transparent now

			// rescale to original if necessary
			this.Width = _currentScreen.Bounds.Width;
			this.Height = _currentScreen.Bounds.Height;

			DoubleAnimation doubleAnimation = new DoubleAnimation()
			{
				From = 0.2,
				To = 0.7,
				Duration = TimeSpan.FromSeconds(_userSettings.WhiteDipAnimDuration),
				AutoReverse = true,
			};
			doubleAnimation.Completed += WhiteDipWindowAnimation_Completed;
			ShadowCanvas.BeginAnimation(InkCanvas.OpacityProperty, doubleAnimation);
		}

		private void WhiteDipWindowAnimation_Completed(object sender, EventArgs e)
		{
			this.Close();
			return;
		}

		private void OnWhiteDipWindowClosed(object sender, EventArgs args)
		{
			Clipboard.SetImage(_cachedSnapShot);
			if(_isSavingToLocal)
			{
				_cachedSnapShot.SaveToLocal(AbsoluteDirectory: _userSettings.ImageFolderPath, extension: "."+_userSettings.SaveFormatPreferred);
			}
			else
			{
				_screenShot.OnSnapped(_currentScreen, _cachedSnapShot);
			}
			_screenShot?.OnClose();
			this.Loaded -= OnWhiteDipWindowLoaded;
			this.Closed -= OnWhiteDipWindowClosed;
		}
	}
}
