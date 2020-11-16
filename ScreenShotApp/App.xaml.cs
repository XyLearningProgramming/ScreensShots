using ScreenShotApp.Utils;
using ScreenShotApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenShotApp
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		// reference to the rootviewmodel instance
		public static RootViewModel Root => App.Current.TryFindResource("RootViewModel") as RootViewModel;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			// default: start from the startup window with button
			StartUpWindow startUpWindow = new StartUpWindow();
			startUpWindow.DataContext = Root.StartUpWindowViewModel;
			startUpWindow.Loaded += Root.StartUpWindowViewModel.OnWindowLoaded;
			startUpWindow.Closed += Root.StartUpWindowViewModel.OnWindowClosed;
			startUpWindow.Show();
			LogSystemShared.LogWriter.WriteLine("App on startup called.");
		}
		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);
			// save usersettings when exit
			UserSettingsManager.Instance.SaveAll();
			LogSystemShared.LogWriter.WriteLine("App on exit called.");
			LogSystemShared.LogWriter.PurgeAll();
		}
	}
}
