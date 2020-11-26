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
		// reference to the rootviewmodel instance
		public static RootViewModel Root => App.Current.TryFindResource("RootViewModel") as RootViewModel;

		private (System.Windows.Forms.Keys modifiers, System.Windows.Forms.Keys mainkey) HookedCaptureShortcut { get; set; }

		protected override void OnStartup(StartupEventArgs e)
		{
			LogSystemShared.LogWriter.WriteLine(Environment.NewLine,verbose: false);
			LogSystemShared.LogWriter.WriteLine("App started", verbose: false);
			LogSystemShared.LogWriter.WriteLine(Environment.NewLine, verbose: false);

			base.OnStartup(e);

			// load color theme
			ThemeHelper.SelectColorTheme((ColorTheme) Enum.Parse(typeof(ColorTheme), UserSettingsManager.Instance.ColorThemePrefered, true));

			Root.OpenStartupWindow.Execute(null);

			//TestStart testWindow = new TestStart();
			//testWindow.Show();

			UserSettingsStruct.ScreenResolutionInferrer.Initialize(); // infer screens resolution in advance to save time

			// hook keyboard shortcut to system
			HookedCaptureShortcut = (UserSettingsManager.Instance.CaptureShortcutModifierKey.ConvertToKeys(), (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(UserSettingsManager.Instance.CaptureShotcutMainKey.Value));
			KeyboardHook.StartHook(HookedCaptureShortcut.modifiers,HookedCaptureShortcut.mainkey);
			KeyboardHook.KeyboardPressed += KeyboardShortcutsCallback;

			LogSystemShared.LogWriter.WriteLine("App on startup called.");
		}

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

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);
			KeyboardHook.KeyboardPressed -= KeyboardShortcutsCallback;
			KeyboardHook.StopHook(HookedCaptureShortcut.modifiers,HookedCaptureShortcut.mainkey);
			// save usersettings when exit
			UserSettingsManager.Instance.SaveAll();
			LogSystemShared.LogWriter.WriteLine("App on exit called.");
			LogSystemShared.LogWriter.PurgeAll();
		}

		public void UpdateShortcutHook()
		{
			var tmp = (UserSettingsManager.Instance.CaptureShortcutModifierKey.ConvertToKeys(), (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(UserSettingsManager.Instance.CaptureShotcutMainKey.Value));
			if(tmp != HookedCaptureShortcut)
			{
				KeyboardHook.StartHook(tmp.Item1, tmp.Item2);
				KeyboardHook.StopHook(HookedCaptureShortcut.modifiers, HookedCaptureShortcut.mainkey);
				HookedCaptureShortcut = tmp;
			}
		}
		public void UnhookCurrentShortcuts()
		{
			KeyboardHook.StopHook(HookedCaptureShortcut.modifiers, HookedCaptureShortcut.mainkey);
		}
		public void RehookCurrentShortcuts()
		{
			KeyboardHook.StartHook(HookedCaptureShortcut.modifiers, HookedCaptureShortcut.mainkey);
		}
	}
}
