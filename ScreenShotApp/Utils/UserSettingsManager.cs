using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.IO;
using System.Linq;
using System.Windows.Markup;
using System.Xml;
using System.Runtime.CompilerServices;
using UserSettingsStruct;
using System.Windows.Input;
using ScreenShotWindows;
using ScreenShotApp.ViewModels;
using System.Diagnostics;

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
		public static UserSettingsManager Instance { get; } 

		#region constructor
		static UserSettingsManager()
		{
			//// not allowing instantiate in design mode
			//if(DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			//	return;
			LocalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xaml");
			AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ThisAppsName, "setttings.xaml");
			Instance = new UserSettingsManager();
		}
		public UserSettingsManager()
		{
			// load settings
			if(!string.IsNullOrWhiteSpace(AppDataPath) && !File.Exists(AppDataPath))
				Directory.CreateDirectory(new FileInfo(AppDataPath).DirectoryName);

			if(!TryLoad(LocalPath, out loadedSettings))
			{
				TryLoad(AppDataPath, out loadedSettings);
			}
			if(loadedSettings == null) loadedSettings = new ResourceDictionary();
			//Application.Current.Resources.MergedDictionaries.Add(loadedSettings);
		}
		#endregion

		#region fields
		private ResourceDictionary loadedSettings;
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
		public WindowsCaptureMode UserCaptureMode { get => (WindowsCaptureMode)GetValue((int)WindowsCaptureMode.Frame); set => SetValue((int)value); }
		public WindowsCaptureScreenTarget UserCaptureScreenTarget { get => (WindowsCaptureScreenTarget)GetValue((int)WindowsCaptureScreenTarget.AllScreens); set => SetValue((int)value); }

		public bool IsShowingReferenceLine { get => GetValue(false); set => SetValue(value); }
		public double WhiteDipAnimDuration { get => GetValue(0.5d); set => SetValue(value); }
		public bool IsShowingWhiteDip { get => GetValue(true); set => SetValue(value); }
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
		private string defaultFolder = Path.Combine(Environment.CurrentDirectory, "images");
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

		public string ColorThemePrefered { get => GetValue(ColorTheme.DefaultColorTheme.ToString());
			set
			{
				if(SetValue(value))
				{
					ThemeHelper.SelectColorTheme((ColorTheme)Enum.Parse(typeof(ColorTheme), value));
				}
			}
		}

		public SaveFormatStrings SaveFormatPreferred { get => (SaveFormatStrings)Enum.Parse(typeof(SaveFormatStrings), GetValue(SaveFormatStrings.png.ToString())); set => SetValue(value.ToString()); }

		public List<WidthHeightInfoModel> CustomWHInfo
		{
			get
			{
				return GetValue("").Split(',').Where(s => !string.IsNullOrEmpty(s)).
					Select(s => s.Split('*')).Where(s => s.Length == 2).Select(WH => new WidthHeightInfoModel() { Width = Convert.ToInt32(WH[0]), Height = Convert.ToInt32(WH[0]) }).ToList();
			}

			set
			{
				SetValue(string.Join(",", value.Select(s => $"{s.Width}*{s.Height}")));
			}
		}
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
				IsShowingWhiteDip = Instance.IsShowingWhiteDip,
				SaveFormatPreferred = Instance.SaveFormatPreferred.ToString(),
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
			}
			catch
			{
				LogSystemShared.LogWriter.WriteLine("Cannot read any usersetting.");
				return false;
			}
		}
		// the saving process happened when app is closing, so technically it cannot be saved in async way in case the program end before saving complete
		public void SaveAll()
		{
			if(loadedSettings!= null)
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

					if(!Directory.Exists(AppDataPath))
						Directory.CreateDirectory(Path.GetDirectoryName(AppDataPath));

					File.Copy(LocalPath, AppDataPath, true);
					LogSystemShared.LogWriter.WriteLine($"saved user setting: {LocalPath} and {AppDataPath}");
				}
				catch(Exception e)
				{
					LogSystemShared.LogWriter.WriteLine($"Cannot save user setting: {e.Message}");
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
		private bool SetValue<T>(T value, [CallerMemberName] string key = "") where T: IEquatable<T>
		{
			if(loadedSettings.Contains(key))
			{
				if(((T)loadedSettings[key]).Equals(value) == false){
					loadedSettings[key] = value;
					OnPropertyChanged(key);
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				loadedSettings.Add(key, value);
				OnPropertyChanged(key);
				return true;
			}
		}

		public ScreenInfoModel GetPreferredResolution(string deviceName)
		{
			var strArr = GetValue("", deviceName.RemoveNonLetters()).Split("*");
			if(strArr.Length == 2)
			{
				try
				{
					return new ScreenInfoModel() { Resolution = (Convert.ToInt32(strArr[0]),Convert.ToInt32(strArr[1])) };
				}
				catch
				{
					return null;
				}
			}
			else return null;
		}
		public bool SetPreferredResolution(string deviceName, ScreenInfoModel screenInfoModel)
		{
			if(screenInfoModel != null)
			{
				// set current res in dict
				var screen = System.Windows.Forms.Screen.AllScreens.Where(s => s.DeviceName.RemoveNonLetters() == screenInfoModel.DeviceName.RemoveNonLetters()).FirstOrDefault();
				if(screen != null)
				{
					ScreenResolutionInferrer.ForceChangeStoredResolution(screen, screenInfoModel.Resolution.width, screenInfoModel.Resolution.height);
					return SetValue($"{screenInfoModel.Resolution.width}*{screenInfoModel.Resolution.height}", deviceName.RemoveNonLetters());
				}
				Debug.Assert(screen != null); // how?
			}
			return false;
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

	public enum SaveFormatStrings { png, jpeg, bmp }
}
