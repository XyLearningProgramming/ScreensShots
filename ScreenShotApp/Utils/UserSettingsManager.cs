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
using UserSettingsStruct;
using System.Windows.Input;

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
		public double WhiteDipAnimDuration { get => GetValue(0.5d); set => SetValue(value); }
		public ModifierKeys CaptureShortcutModifierKey 
		{
			get => (ModifierKeys)GetValue((int)ModifierKeys.Alt);
			set => SetValue((int)value);
		}
		public Key? CaptureShotcutMainKey
		{
			get
			{
				Key storedKey = (Key)GetValue((int)Key.S);
				return storedKey == Key.None ? null : storedKey as Key?;
			}
			set
			{
				if(!value.HasValue) SetValue((int)Key.None);
				else SetValue((int)value);
			}
		}

		/// <summary>
		/// Get guarantees an useable folder, while set don't check
		/// </summary>
		private string defaultFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
		public string ImagesFolderAbsolutePath 
		{
			get
			{
				string folder = GetValue(defaultFolder);
				if(!Directory.Exists(folder))
				{
					try
					{
						Directory.CreateDirectory(folder);
					}
					catch
					{
						folder = defaultFolder;
					}
				}
				return folder;
			}
			set => SetValue(value);
		}

		public string ColorThemePrefered { get => GetValue(ColorTheme.DefaultColorTheme.ToString()); set => SetValue(value); }
		#endregion
		#endregion

		#region methods
		public static UserSettingsForScreenShotWindows ConstructSettings()
		{
			return new UserSettingsForScreenShotWindows() 
			{ 
				IsShowingReferenceLine = Instance.IsShowingReferenceLine, 
				WhiteDipAnimDuration = Instance.WhiteDipAnimDuration,
				ImageFolderPath = Instance.ImagesFolderAbsolutePath,
			};
		}

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

	public static class ModifierKeyToKeysExt
	{
		public static System.Windows.Forms.Keys ConvertToKeys(this System.Windows.Input.ModifierKeys modifier)
		{
			System.Windows.Forms.Keys result = System.Windows.Forms.Keys.None;
			if(modifier.HasFlag(System.Windows.Input.ModifierKeys.Control))
			{
				result |= System.Windows.Forms.Keys.Control;
			}
			else if(modifier.HasFlag(System.Windows.Input.ModifierKeys.Shift))
			{
				result |= System.Windows.Forms.Keys.Shift;
			}
			else if(modifier.HasFlag(System.Windows.Input.ModifierKeys.Alt))
			{
				result |= System.Windows.Forms.Keys.Alt;
			}
			return result;
		}
	}
}
