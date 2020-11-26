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
		private string captureShortcutString = $"{KeyStringHelper.GetSelectKeyText((System.Windows.Input.Key)UserSettingsManager.Instance.CaptureShotcutMainKey, UserSettingsManager.Instance.CaptureShortcutModifierKey,true,true)}";
		#endregion

		#region properties
		//public bool IsCapturing { get => capturingWindowCount != 0; }
		public RootViewModel Root { get; set; }
		
		public string CaptureShortcutString { get => captureShortcutString; set => this.MutateVerbose(ref captureShortcutString, value, e => PropertyChanged?.Invoke(this, e)); }
		#endregion
		public StartUpWindowViewModel(RootViewModel rootViewModel)
		{
			this.Root = rootViewModel;
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
		#endregion
	}

}
