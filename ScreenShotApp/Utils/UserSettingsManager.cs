using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.IO;
using System.Windows.Markup;
using System.Xml;
using System.Runtime.CompilerServices;

namespace ScreenShotApp.Utils
{
	/// <summary>
	/// A singleton used to read and overwrite users' preferences. Automatically save when closed and load when app opened
	/// It stores all data in xaml format and load them in a private .
	/// </summary>
	internal sealed class UserSettingsManager : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		// singleton
		public static UserSettingsManager Instance { get; } = new UserSettingsManager();

		#region constructor
		static UserSettingsManager()
		{
			// not allowing instantiate in design mode
			if(DesignerProperties.GetIsInDesignMode(new DependencyObject()))
				return;
			LocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xaml");
			AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ThisAppsName, "setttings.xaml");
		}
		public UserSettingsManager()
		{
			// load settings
			if(!string.IsNullOrWhiteSpace(AppDataPath) && !Directory.Exists(AppDataPath))
					Directory.CreateDirectory(AppDataPath);

			if(!TryLoad(LocalPath, out loadedSettings))
			{
				TryLoad(AppDataPath, out loadedSettings);
			}
			//Application.Current.Resources.MergedDictionaries.Add(loadedSettings);
		}
		#endregion

		#region fields
		private ResourceDictionary loadedSettings;
		#endregion

		#region methods

		#endregion

		#region properties

		#region application
		public static string ThisAppsName { get; } = "ScreenShotApp";
		public static string Version { get; } = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.1";
		public static string LocalPath { get; private set; } 
		// in application data folder, acting as a backup place for settings
		public static string AppDataPath { get; }
		#endregion

		#region ScreenShotWindows
		public bool IsShowingReferenceLine { get => GetValue(false); set => SetValue(value); }
		#endregion
		#endregion

		#region methods
		private bool TryLoad(string path, out ResourceDictionary loadedSettings)
		{
			loadedSettings = default(ResourceDictionary);
			try
			{
				if(!File.Exists(path)) return false;
				using(FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					loadedSettings = (ResourceDictionary)XamlReader.Load(fs);
				}
				return true;
			}catch (Exception e)
			{
				//TODO log this exception
				return false;
			}
		}
		// the saving process happened when app is closing, so technically it cannot be saved in async way in case the program end before saving complete
		public void SaveAll()
		{
			if(loadedSettings!= default(ResourceDictionary))
			{
				try
				{
					var settings = new XmlWriterSettings
					{
						Indent = true,
						CheckCharacters = true,
						CloseOutput = true,
						ConformanceLevel = ConformanceLevel.Fragment,
						Encoding = Encoding.UTF8,
					};

					using(var writer = XmlWriter.Create(LocalPath, settings))
						XamlWriter.Save(loadedSettings, writer);

					File.Copy(LocalPath, AppDataPath, true);
				}
				catch(Exception e)
				{
					//TODO: log this exception
				}
			}
		}
		private T GetValue<T>(T defaultValue, [CallerMemberName] string key = "")
		{
			if(loadedSettings!=default(ResourceDictionary) && loadedSettings.Contains(key))
			{
				return (T)loadedSettings[key];
			}
			else
				return defaultValue;
		}
		private void SetValue<T>(T value, [CallerMemberName] string key = "") where T: IEquatable<T>
		{
			if(loadedSettings!= default(ResourceDictionary))
			{
				if(loadedSettings.Contains(key))
				{
					if(((T)loadedSettings[key]).Equals(value) == false){
						loadedSettings[key] = value;
						OnPropertyChanged(key);
					}
				}
				else
				{
					loadedSettings.Add(key, value);
					OnPropertyChanged(key);
				}
			}
		}
		#endregion
	}
}
