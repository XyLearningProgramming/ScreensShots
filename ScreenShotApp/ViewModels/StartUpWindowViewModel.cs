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
		private UserSettingsForScreenShotWindows userSettings;
		private int capturingWindowCount = 0;
		#endregion

		#region properties
		public bool IsCapturing { get => capturingWindowCount != 0; }
		private RootViewModel Root { get; set; }

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
		private void CaptureWindowCloseCallback(object sender, EventArgs e) => capturingWindowCount--;
		#endregion

		#region Commands
		private DelegateCommand startCapturing;

		public DelegateCommand StartCapturing
		{
			get => startCapturing ?? (startCapturing =
				new DelegateCommand(
					(_) => 
					{
						capturingWindowCount++;
						//MessageBox.Show("CommandExecuted");
						ScreenShot.Closed += CaptureWindowCloseCallback;
						ScreenShot ss = new ScreenShot();
						ss.Start(WindowsCaptureScreenTarget.AllScreens, WindowsCaptureMode.Frame, userSettings);
					},
				// not allowing multiple capture at the same time for now
				(_)=>{ return !this.IsCapturing; }
				));
		}
		#endregion
	}

}
