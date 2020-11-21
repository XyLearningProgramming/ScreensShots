using ScreenShotApp.Utils;
using ScreenShotApp.MVVMUtils;
using ScreenShotWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using UserSettingsStruct;
using System.Linq;

namespace ScreenShotApp.ViewModels
{
	public class StartUpWindowViewModel: INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		#region fields
		private RootViewModel Root;
		private bool isCapturing = false;
		private UserSettingsForScreenShotWindows userSettings;
		#endregion

		#region properties

		#endregion
		public StartUpWindowViewModel(RootViewModel rootViewModel)
		{
			this.Root = rootViewModel;
			// TODO adjust this via config file & system
			userSettings = new UserSettingsForScreenShotWindows() { IsShowingReferenceLine = UserSettingsManager.Instance.IsShowingReferenceLine };
		}

		#region public functions
		public void OnWindowLoaded(object sender, RoutedEventArgs args)
		{

		}
		public void OnWindowClosed(object sender, EventArgs args)
		{

		}

		#endregion

		#region Commands
		private DelegateCommand startCapturing;

		public DelegateCommand StartCapturing
		{
			get => startCapturing ??
				new DelegateCommand(
					(_) => 
					{
						isCapturing = true;
						//MessageBox.Show("CommandExecuted");
						new ScreenShot().Start(WindowsCaptureScreenTarget.MainScreen, WindowsCaptureMode.Frame, userSettings);
					},
				// not allowing multiple capture at the same time
				(_)=>{ return !this.isCapturing; }
				);
		}
		#endregion
	}

}
