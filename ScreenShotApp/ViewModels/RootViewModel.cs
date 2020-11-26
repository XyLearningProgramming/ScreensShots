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
		//private Dictionary<IMyWindows, Style> _myWindowsStyle = new Dictionary<IMyWindows, Style>(); // change win style to null to cancel the minimize and max animation
		#endregion

		#region properties
		public StartUpWindowViewModel StartUpWindowViewModel { get => startUpWindowViewModel; }
		public OptionsWindowViewModel OptionsWindowViewModel { get => optionsWindowViewModel; }
		#endregion

		#region constructors
		public RootViewModel()
		{
			startUpWindowViewModel = new StartUpWindowViewModel(this);
			optionsWindowViewModel = new OptionsWindowViewModel(this);
		}
		#endregion

		#region methods
		private void CaptureWindowCloseCallback(object sender, EventArgs e)
		{
			_isCapturing = false;
			foreach(var pr in _myWindowsStates)
			{
				pr.Normalize();
			}
			//_myWindowsStyle.Clear();
			ScreenShot.AllClosed -= CaptureWindowCloseCallback;
			((App)Application.Current).RehookCurrentShortcuts();
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
					opWin.DataContext = this.OptionsWindowViewModel;
					opWin.Loaded += this.OptionsWindowViewModel.OnWindowLoaded;
					opWin.Loaded += (s, e) => ((App)Application.Current).UnhookCurrentShortcuts(); // prevent infinite loop when resetting options
					opWin.Closed += this.OptionsWindowViewModel.OnWindowClosed;
					opWin.Closed += (s, e) => { _myWindowsStates.Remove(opWin); };
					opWin.Closed += (s, e) => ((App)Application.Current).RehookCurrentShortcuts();
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

		private DelegateCommand _openCaptureWindow;
		public DelegateCommand OpenCaptureWindow {get => _openCaptureWindow ??
				(_openCaptureWindow = new DelegateCommand(
					(_) =>
					{
						_isCapturing = true;
						((App)Application.Current).UnhookCurrentShortcuts();
						foreach(var myWindow in _myWindowsStates)
						{
							//_myWindowsStyle.Add(myWindow, myWindow.GetStyle());
							//myWindow.SetStyle(null);
							myWindow.Minimize();
						}
						ScreenShot.AllClosed += CaptureWindowCloseCallback;
						ScreenShot ss = new ScreenShot();
						Task.Delay(150).ContinueWith(_ =>
					   {
						   Application.Current.Dispatcher.Invoke(() =>
						   {
							   ss.Start(WindowsCaptureScreenTarget.MainScreen, WindowsCaptureMode.Frame, UserSettingsManager.ConstructSettings());
						   });
					   });
					},
					(_) => !_isCapturing
					)); }

		private DelegateCommand _openFileExplorer;
		public DelegateCommand OpenFileExplorer { get => _openFileExplorer ?? (_openFileExplorer = new DelegateCommand(
			(_) => 
			{
				string imageFolderPath = UserSettingsManager.Instance.ImagesFolderAbsolutePath;
				Process.Start("explorer.exe", imageFolderPath);
			})); }
		#endregion
	}
}
