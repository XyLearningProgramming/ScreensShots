using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Linq;

namespace ScreenShotApp.Utils
{
	public class ThemeHelper
	{
		public static void SelectColorTheme(ColorTheme theme = ColorTheme.DefaultColorTheme)
		{
			ResourceDictionary current = Application.Current.Resources.MergedDictionaries.LastOrDefault(s => s.Source != null && s.Source.ToString().Contains(@"/Themes/Colors/"));
			if(current != null && current.Source.ToString().EndsWith($"{theme}.xaml")) return;

			ResourceDictionary target = Application.Current.Resources.MergedDictionaries.LastOrDefault(s => s.Source != null && s.Source.ToString().Contains($@"/Themes/Colors/{theme}.xaml"));
			if(target == null)
			{
				theme = ColorTheme.DefaultColorTheme;
				target = Application.Current.Resources.MergedDictionaries.LastOrDefault(s => s.Source != null && s.Source.ToString().Contains($@"/Themes/Colors/{theme}.xaml"));
			}
			Application.Current.Resources.MergedDictionaries.Remove(target);
			Application.Current.Resources.MergedDictionaries.Add(target);

			ResourceDictionary iconsDict = Application.Current.Resources.MergedDictionaries.LastOrDefault(s => s.Source != null && s.Source.ToString().Contains($@"/Themes/Icons.xaml"));
			if(iconsDict != null)
			{
				Application.Current.Resources.MergedDictionaries.Remove(iconsDict);
				Application.Current.Resources.MergedDictionaries.Add(iconsDict);
			}

			//UserSettingsManager.Instance.ColorThemePrefered = theme.ToString();
			LogSystemShared.LogWriter.WriteLine($"Switched Theme to {theme}");
		}

	}
	public enum ColorTheme 
	{ 
		DefaultColorTheme,
		DarkColorTheme,
	}
}
