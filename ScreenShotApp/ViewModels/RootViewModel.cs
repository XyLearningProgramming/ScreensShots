using ScreenShotApp.MVVMUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Linq;
using ScreenShotWindows;
using ScreenShotApp.Utils;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ScreenShotApp.ViewModels
{
	public class RootViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		#region fields
		private StartUpWindowViewModel startUpWindowViewModel;
		private OptionsWindowViewModel optionsWindowViewModel;
		private bool _isCapturing = false;
		private List<IMyWindows> _myWindowsStates = new List<IMyWindows>();
		private ScreenShot _onlyScreenShot;
		//private Dictionary<IMyWindows, Style> _myWindowsStyle = new Dictionary<IMyWindows, Style>(); // change win style to null to cancel the minimize and max animation
		#endregion

		#region properties
		public StartUpWindowViewModel StartUpWindowViewModel { get => startUpWindowViewModel; }
		public OptionsWindowViewModel OptionsWindowViewModel { get => optionsWindowViewModel; }
		#endregion

		#region constructors
		public RootViewModel()
		{
			// read resolution preferred settings and force change them 
			foreach(var screen in System.Windows.Forms.Screen.AllScreens)
			{
				var info = UserSettingsManager.Instance.GetPreferredResolution(screen.DeviceName);
				if(info == null) continue;
				else
				{
					UserSettingsStruct.ScreenResolutionInferrer.ForceChangeStoredResolution(screen, info.Resolution.width, info.Resolution.height);
				}
			}

			startUpWindowViewModel = new StartUpWindowViewModel(this);
			optionsWindowViewModel = new OptionsWindowViewModel(this);
		}
		#endregion

		#region methods
		private void CaptureWindowCloseCallback(object sender, EventArgs e)
		{
			if(_onlyScreenShot!=null && sender is ScreenShot sh && sh == _onlyScreenShot)
			{
				_isCapturing = false;
				foreach(var pr in _myWindowsStates)
				{
					pr.TryRestoreWindowState();
				}
				//_myWindowsStyle.Clear();
				ScreenShot.AllClosed -= CaptureWindowCloseCallback;
				((App)Application.Current).RehookCurrentShortcuts();
			}
		}
		#endregion

		#region commands
		private DelegateCommand _openStartupWindow;
		public DelegateCommand OpenStartupWindow { get => _openStartupWindow ?? (
				 _openStartupWindow = new DelegateCommand(
					 (_) =>
					 {
						 var startup = Application.Current.Windows.OfType<StartUpWindow>().FirstOrDefault();
						 if(startup == null)
						 {
							 // default: start from the startup window with button
							 startup = new StartUpWindow();
							 startup.DataContext = this.StartUpWindowViewModel;
							 startup.Loaded += this.StartUpWindowViewModel.OnWindowLoaded;
							 startup.Closed += this.StartUpWindowViewModel.OnWindowClosed;
							 startup.Closed += (s, e) => { _myWindowsStates.Remove(startup); };
							 startup.Closed += (s, e) => { App.TryExit(); }; // check if can exit
							 startup.Show();

							 _myWindowsStates.Add(startup);
						 }
						 else
						 {
							 if(startup.WindowState == WindowState.Minimized)
							 {
								 startup.WindowState = WindowState.Normal;
							 }
						 }
					 })); }

		private DelegateCommand _openOptionsWindow;
		public DelegateCommand OpenOptionsWindow
		{
			get => _openOptionsWindow ?? (
				_openOptionsWindow = new DelegateCommand(
				(_) =>
				{
					var opWin = Application.Current.Windows.OfType<OptionWindow>().FirstOrDefault();
				if(opWin == null)
				{
					// default: start from the startup window with button
					opWin = new OptionWindow();
					// opWin.Owner = Application.Current.Windows.OfType<StartUpWindow>().FirstOrDefault(); // option window is belonged to the start up one ?? should it?
					opWin.DataContext = this.OptionsWindowViewModel;
					opWin.Loaded += this.OptionsWindowViewModel.OnWindowLoaded;
					opWin.Loaded += (s, e) => ((App)Application.Current).UnhookCurrentShortcuts(); // prevent infinite loop when resetting options
					opWin.Closed += this.OptionsWindowViewModel.OnWindowClosed;
					opWin.Closed += (s, e) => { _myWindowsStates.Remove(opWin); };
					opWin.Closed += (s, e) => ((App)Application.Current).RehookCurrentShortcuts();
					opWin.Closed += (s, e) => { App.TryExit(); }; // check if can exit
					opWin.Show();

					_myWindowsStates.Add(opWin);
					}
					else
					{
						if(opWin.WindowState == WindowState.Minimized)
						{
							opWin.WindowState = WindowState.Normal;
						}
				}
				}));
		}

		//private DelegateCommand _openCaptureWindow;
		public DelegateCommand OpenCaptureWindow {get => new DelegateCommand(
					(_) =>
					{
						_isCapturing = true;
						((App)Application.Current).UnhookCurrentShortcuts();
						foreach(var myWindow in _myWindowsStates)
						{
							//_myWindowsStyle.Add(myWindow, myWindow.GetStyle());
							//myWindow.SetStyle(null);
							myWindow.MinimizeWindowState();
						}
						_onlyScreenShot = new ScreenShot();
						ScreenShot.AllClosed += CaptureWindowCloseCallback;
						Task.Delay(150).ContinueWith(_ =>
					   {
						   Application.Current.Dispatcher.Invoke(() =>
						   {
							   _onlyScreenShot.Start(UserSettingsManager.Instance.UserCaptureScreenTarget, UserSettingsManager.Instance.UserCaptureMode, UserSettingsManager.ConstructSettings());
						   });
					   });
					},
					(_) => !_isCapturing
					); }

		//private DelegateCommand _openFileExplorer;
		public DelegateCommand OpenFileExplorer { get => new DelegateCommand(
			(_) => 
			{
				string imageFolderPath = UserSettingsManager.Instance.ImagesFolderAbsolutePath;
				Process.Start("explorer.exe", imageFolderPath);
			}); }

		#endregion
	}
}
