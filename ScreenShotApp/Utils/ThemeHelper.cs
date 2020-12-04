using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using ScreenShotApp.Controls;

namespace ScreenShotApp.Utils
{
	public class ThemeHelper
	{
		private static ResourceDictionary iconsLight;
		private static ResourceDictionary iconsDark;
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

			if(iconsLight==null && iconsDark == null)
			{
				iconsLight = Application.Current.Resources.MergedDictionaries.LastOrDefault(s => s.Source != null && s.Source.ToString().Contains($@"/Themes/Icons.xaml"));
				iconsDark = Application.Current.Resources.MergedDictionaries.LastOrDefault(s => s.Source != null && s.Source.ToString().Contains($@"/Themes/Icons_Dark.xaml"));
				Application.Current.Resources.MergedDictionaries.Remove(iconsLight);
				Application.Current.Resources.MergedDictionaries.Remove(iconsDark);
			}
			else
			{
				Application.Current.Resources.MergedDictionaries.Remove(theme == ColorTheme.DarkColorTheme ? iconsDark : iconsLight);
			}
			Application.Current.Resources.MergedDictionaries.Add(theme == ColorTheme.DarkColorTheme? iconsLight: iconsDark);

			//// FOR unkown reason, this is not working. So frustrating...
			//Application.Current.Resources["IconDefaultBrush"] = new SolidColorBrush(theme == ColorTheme.DefaultColorTheme ? Color.FromArgb(0xFF, 0x20, 0x20, 0x20) : Colors.White);
			////var brush = Application.Current.Resources["IconDefaultBrush"] as SolidColorBrush;
			////brush.Color = theme == ColorTheme.DefaultColorTheme ? Colors.Gray : Colors.White;

			//Application.Current.Resources.MergedDictionaries.Remove(iconsDict);
			//Application.Current.Resources.MergedDictionaries.Add(iconsDict);


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
