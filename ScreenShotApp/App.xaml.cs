using ScreenShotApp.MVVMUtils;
using ScreenShotApp.Utils;
using ScreenShotApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ScreenShotApp
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		#region properties
		// reference to the rootviewmodel instance
		public static RootViewModel Root => App.Current.TryFindResource("RootViewModel") as RootViewModel;

		private (System.Windows.Forms.Keys modifiers, System.Windows.Forms.Keys mainkey) HookedCaptureShortcut { get; set; }
		#endregion

		#region global shortcuts
		private void KeyboardShortcutsCallback(object sender, KeyboardHook.KeyBoardEventArgs e)
		{
			if((e.Modifier, e.Mainkey) == HookedCaptureShortcut)
			{
				LogSystemShared.LogWriter.WriteLine("Keyboard capture shortcut pressed");
				if(Root.OpenCaptureWindow.CanExecute(null))
				{
					Root.OpenCaptureWindow.Execute(null);
				}
			}
		}

		public void UpdateShortcutHook()
		{
			HookedCaptureShortcut = (UserSettingsManager.Instance.CaptureShortcutModifierKey.ConvertToKeys(), (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(UserSettingsManager.Instance.CaptureShotcutMainKey.Value));
		}
		public void UnhookCurrentShortcuts()
		{
			KeyboardHook.StopHook(HookedCaptureShortcut.modifiers, HookedCaptureShortcut.mainkey);
		}
		public void RehookCurrentShortcuts()
		{
			KeyboardHook.StartHook(HookedCaptureShortcut.modifiers, HookedCaptureShortcut.mainkey);
		}
		#endregion

		#region app start and close callbacks
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			LogSystemShared.LogWriter.WriteLine(Environment.NewLine, verbose: false);
			LogSystemShared.LogWriter.WriteLine("App started", verbose: false);
			LogSystemShared.LogWriter.WriteLine(Environment.NewLine, verbose: false);

			// load color theme
			ThemeHelper.SelectColorTheme((ColorTheme)Enum.Parse(typeof(ColorTheme), UserSettingsManager.Instance.ColorThemePrefered, true));


			//TestStart testWindow = new TestStart();
			//testWindow.Show();

			// hook keyboard shortcut to system
			HookedCaptureShortcut = (UserSettingsManager.Instance.CaptureShortcutModifierKey.ConvertToKeys(), (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(UserSettingsManager.Instance.CaptureShotcutMainKey.Value));
			KeyboardHook.StartHook(HookedCaptureShortcut.modifiers, HookedCaptureShortcut.mainkey);
			KeyboardHook.KeyboardPressed += KeyboardShortcutsCallback;

			LogSystemShared.LogWriter.WriteLine("App on startup called.");

			Root.OpenStartupWindow.Execute(null);
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			KeyboardHook.StopHook(HookedCaptureShortcut.modifiers, HookedCaptureShortcut.mainkey);
			KeyboardHook.KeyboardPressed -= KeyboardShortcutsCallback;

			// save current screen settings for all monitors
			// suppose it's correct message 
			foreach(var sc in System.Windows.Forms.Screen.AllScreens)
			{
				var infoModel = new ScreenInfoModel() { DeviceName=sc.DeviceName, Resolution = UserSettingsStruct.ScreenResolutionInferrer.GetInferredResolution(sc) };
				UserSettingsManager.Instance.SetPreferredResolution(sc.DeviceName, infoModel);
			}

			// save usersettings when exit
			UserSettingsManager.Instance.SaveAll();
			LogSystemShared.LogWriter.WriteLine("App on exit called.");
			LogSystemShared.LogWriter.PurgeAll();
		}
		#endregion

		#region exit command
		private DelegateCommand _exitApplicationCommand;
		public DelegateCommand ExitApplicationCommand {get => _exitApplicationCommand ?? (
				_exitApplicationCommand = new DelegateCommand(
					(_) =>
					{
						this.Shutdown();
					},
					(_) =>
					{
						// this app is only ready to close when none of OptionWindow or StartUpWindow exists.
						return this.Windows.OfType<OptionWindow>().Any() == false && this.Windows.OfType<StartUpWindow>().Any() == false;
					}));}

		/// <summary>
		/// Controls the time of exit of this process. Called when any custom window is closed
		/// </summary>
		public static void TryExit()
		{
			if(Application.Current is App app && app.ExitApplicationCommand.CanExecute(null)==true)
			{
				app.ExitApplicationCommand.Execute(null);
			}
		}
		#endregion
	}
}
