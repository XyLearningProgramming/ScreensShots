using ScreenShotApp.Utils;
using ScreenShotApp.MVVMUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ScreenShotApp.Controls;
using ScreenShotWindows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using UserSettingsStruct;
using System.Diagnostics;
using ScreenShotApp.Windows;

namespace ScreenShotApp.ViewModels
{
	public class OptionsWindowViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private RootViewModel Root { get; set; }
		public ResolutionSelectorViewModel ResolutionSelectorViewModel { get; set; }
		public OptionsWindowViewModel(RootViewModel rootViewModel)
		{
			this.Root = rootViewModel;
			ResolutionSelectorViewModel = new ResolutionSelectorViewModel(this);

		}

		// used ONLY in design time
		public OptionsWindowViewModel() { }

		#region public functions
		public void OnWindowLoaded(object sender, RoutedEventArgs args)
		{
			UpdateImageFolderInfo();
			UpdateScreenResolutionPanel();
		}
		public void OnWindowClosed(object sender, EventArgs args)
		{

		}
		#endregion

		#region capture mode panel
		private ModifierKeys _captureShortcutModifierKey = UserSettingsManager.Instance.CaptureShortcutModifierKey;
		public ModifierKeys CaptureShortcutModifierKey { get => _captureShortcutModifierKey;  
			set 
			{
				if(this.MutateVerbose(ref _captureShortcutModifierKey, value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.CaptureShortcutModifierKey = value;
				}
			} 
		}
		private Key? _captureShortcutMainKey = UserSettingsManager.Instance.CaptureShotcutMainKey;
		public Key? CaptureShortcutMainKey { get => _captureShortcutMainKey;
			set
			{
				if(this.MutateVerbose(ref _captureShortcutMainKey, value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.CaptureShotcutMainKey = value;
				}
			}
		}
		private WindowsCaptureScreenTarget _windowsCaptureScreenTarget = UserSettingsManager.Instance.UserCaptureScreenTarget;
		public WindowsCaptureScreenTarget WindowsCaptureScreenTarget { get => _windowsCaptureScreenTarget;
			set 
			{
				if(this.MutateVerbose(ref _windowsCaptureScreenTarget, value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.UserCaptureScreenTarget = value;
				}
			}
		}
		private WindowsCaptureMode _windowsCaptureMode = UserSettingsManager.Instance.UserCaptureMode;
		public WindowsCaptureMode WindowsCaptureMode { get => _windowsCaptureMode;
			set
			{
				if(this.MutateVerbose(ref _windowsCaptureMode, value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.UserCaptureMode = value;
				}
			}
		}
		private bool _isShowingWhiteDip = UserSettingsManager.Instance.IsShowingWhiteDip;
		public bool IsShowingWhiteDip { get => _isShowingWhiteDip;
			set 
			{
				if(this.MutateVerbose(ref _isShowingWhiteDip, value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.IsShowingWhiteDip = value;
				}
			}
		}
		#endregion

		#region screen res panel
		private ObservableCollection<ScreenInfoModel> _screensInfo = new ObservableCollection<ScreenInfoModel>();
		public ObservableCollection<ScreenInfoModel> ScreensInfo { get => _screensInfo; set => _screensInfo = value; }

		private ScreenShot tmpScreenShot; // saved for checking callbacks
		private string _selectorTargetDeviceName = default(string); // used for cache selector view model's currently binding screen
		/// <summary>
		/// Create a snapshot for all screens
		/// </summary>
		private void UpdateScreenResolutionPanel()
		{
			ScreenInfoModel.TotalScreenIndex = 0;
			ScreensInfo.Clear();
			ScreenShot.AllClosed += AllClosedCallback;
			ScreenShot.Snapped += OnSnapOneScreenCallback;
			tmpScreenShot = new ScreenShot();
			tmpScreenShot.Start(WindowsCaptureScreenTarget.AllScreens, WindowsCaptureMode.Continuous, new UserSettingsStruct.UserSettingsForScreenShotWindows() { IsShowingWhiteDip = false, WhiteDipAnimDuration = 0 }, false);
		}
		private void AllClosedCallback(object sender, EventArgs args)
		{
			if(sender is ScreenShot screenShot_ && screenShot_ == tmpScreenShot)
			{
				ScreenShot.AllClosed -= AllClosedCallback;
				ScreenShot.Snapped -= OnSnapOneScreenCallback;
				tmpScreenShot = null;
			}
		}
		private void OnSnapOneScreenCallback(object sender, (System.Windows.Forms.Screen screen, BitmapSource image) args)
		{
			if(sender is ScreenShot screenShot_ && screenShot_ == tmpScreenShot)
			{
				ScreensInfo.Add(new ScreenInfoModel() { SampleShot = args.image, Resolution = ScreenResolutionInferrer.GetInferredResolution(args.screen), ScaleFactor = ScreenResolutionInferrer.GetInferredScale(args.screen), DeviceName= args.screen.DeviceName });

				LogSystemShared.LogWriter.WriteLine($"screensinfo in viewmodel added {args.screen.DeviceName}");

				// update selector viewmodel if possible
				if(_selectorTargetDeviceName!=null && !string.IsNullOrEmpty(_selectorTargetDeviceName) && args.screen.DeviceName==_selectorTargetDeviceName)
				{
					ResolutionSelectorViewModel.UpdateScreenInfo(ScreensInfo.Last());
				}
			}
		}
		#endregion

		#region others panel
		private SaveFormatStrings _preferredSaveFormat = UserSettingsManager.Instance.SaveFormatPreferred;
		public SaveFormatStrings PreferredSaveFormat { get => _preferredSaveFormat;
			set 
			{
				if(this.MutateVerbose(ref _preferredSaveFormat, value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.SaveFormatPreferred = value;
				}
			}
		}

		private string _imagesPath = UserSettingsManager.Instance.ImagesFolderAbsolutePath;
		public string ImagesPath
		{
			get => _imagesPath;
			set
			{
				if(this.MutateVerbose(ref _imagesPath, value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.ImagesFolderAbsolutePath = value;
				}
			}
		}
		private int _imagesInFolderCount;
		public int ImagesInFolderCount { get => _imagesInFolderCount; set => this.MutateVerbose(ref _imagesInFolderCount, value, e => PropertyChanged?.Invoke(this, e)); }
		private double _imagesFolderSizeMB;
		public double ImagesFolderSizeMB { get => _imagesFolderSizeMB; set => this.MutateVerbose(ref _imagesFolderSizeMB, value, e => PropertyChanged?.Invoke(this, e)); }
		private string _diskSpaceInfo = ""; //30/60GB (50%)
		public string DiskSpaceInfo { get => _diskSpaceInfo; set=> this.MutateVerbose(ref _diskSpaceInfo, value, e => PropertyChanged?.Invoke(this, e)); }

		private string[] _imageExtensions = new string[] { "jpg", "jpeg", "png", "bmp" };
		private void UpdateImageFolderInfo()
		{
			string currPath = UserSettingsManager.Instance.ImagesFolderAbsolutePath;
			List<string> imagesFoundStrs = new List<string>();
			foreach(string extension in _imageExtensions)
			{
				imagesFoundStrs.AddRange(Directory.EnumerateFiles(currPath, extension, SearchOption.AllDirectories));
			}
			ImagesInFolderCount = imagesFoundStrs.Count;
			ImagesFolderSizeMB = Convert.ToDouble(imagesFoundStrs.Select(a => new FileInfo(a).Length / (1024 * 1024)).Sum());

			var driveInfo = new DriveInfo(Path.GetPathRoot(currPath));
			long freeSpace = driveInfo.AvailableFreeSpace / (1024 * 1024 * 1024);
			long totalSpace = driveInfo.TotalSize / (1024 * 1024 * 1024);
			StringBuilder sb = new StringBuilder();
			sb.Append(freeSpace.ToString("0.##"));
			sb.Append("/");
			sb.Append(totalSpace.ToString("0.##"));
			sb.Append("GB ");
			sb.Append($"({Convert.ToInt32(freeSpace * 100 / totalSpace)}%)");
			DiskSpaceInfo = sb.ToString();
		}

		private string _currentColorTheme = UserSettingsManager.Instance.ColorThemePrefered;
		public string CurrentColorTheme { get => _currentColorTheme; set 
			{ 
				if(this.MutateVerbose(ref _currentColorTheme,value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.ColorThemePrefered = value;
				}
			} }

		private bool _isShowingReferenceLines = UserSettingsManager.Instance.IsShowingReferenceLine;
		public bool IsShowingReferenceLines { get => _isShowingReferenceLines;
			set 
			{
				if(this.MutateVerbose(ref _isShowingReferenceLines, value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.IsShowingReferenceLine = value;
				}
			}
		}
		private double _whiteDipDuration = UserSettingsManager.Instance.WhiteDipAnimDuration;
		public double WhiteDipDuration { get => _whiteDipDuration;
			set 
			{
				if(this.MutateVerbose(ref _whiteDipDuration, value, e => PropertyChanged?.Invoke(this, e)))
				{
					UserSettingsManager.Instance.WhiteDipAnimDuration = value;
				}
			}
		}
		#endregion

		#region commands
		public class ShortcutChangeArgs 
		{
			public KeyChangedEventArgs e;
			public ShortcutName target;
			public enum ShortcutName { Capture }
		}
		private DelegateCommand _shortcutChangeCommand;
		public DelegateCommand ShortcutChangeCommand
		{
			get => _shortcutChangeCommand ?? (
				_shortcutChangeCommand = new DelegateCommand(
					(object param) =>
					{
						if(param is ShortcutChangeArgs args)
						{
							if(args.target == ShortcutChangeArgs.ShortcutName.Capture)
							{
								if(args.e.CurrentKey != CaptureShortcutMainKey)
								{
									// try update shortcuts 
									CaptureShortcutMainKey = args.e.CurrentKey;
									CaptureShortcutModifierKey = args.e.CurrentModifiers;
									((App)Application.Current).UpdateShortcutHook();
								}
							}
						}
					})); 
		}
		public DelegateCommand ChooseImagesPathCommand { get => new DelegateCommand(
				(_) =>
				{
					var dlg = new CommonOpenFileDialog();
					dlg.Title = "Image Cache Folder";
					dlg.IsFolderPicker = true;
					dlg.InitialDirectory = Directory.GetParent(ImagesPath).FullName;
					dlg.DefaultDirectory = dlg.InitialDirectory;
					dlg.AddToMostRecentlyUsedList = false;
					dlg.AllowNonFileSystemItems = false;
					dlg.EnsurePathExists = true;
					dlg.EnsureFileExists = true;
					dlg.Multiselect = false;
					dlg.EnsureReadOnly = false;
					dlg.ShowPlacesList = true;
					dlg.EnsureValidNames = true;

					if(dlg.ShowDialog()== CommonFileDialogResult.Ok)
					{
						this.ImagesPath = dlg.FileName;
						UpdateImageFolderInfo();
					}
				}
			); }
		//private DelegateCommand _confirmOptionsCommand;
		//public DelegateCommand ConfirmOptionsCommand
		//{
		//	get => _confirmOptionsCommand ?? (_confirmOptionsCommand = new DelegateCommand(
		//		(_) => 
		//		{
		//			//Root.StartUpWindowViewModel.CaptureShortcutString = $"{KeyStringHelper.GetSelectKeyText((System.Windows.Input.Key)UserSettingsManager.Instance.CaptureShotcutMainKey, UserSettingsManager.Instance.CaptureShortcutModifierKey, true, true)}";
		//		}
		//		));
		//}
		public DelegateCommand ChooseScreenResolutionCommand {get =>
			new DelegateCommand(
				(param) =>
				{
					if(param is int si)
					{
						ScreenInfoModel info = ScreensInfo.Where(s => s.ScreenIndex == si).FirstOrDefault();
						Debug.Assert(info != null);
						if(info != null)
						{
							ResolutionSelectorViewModel.UpdateScreenInfo(info);
							ResolutionSelector resWin = App.Current.Windows.OfType<ResolutionSelector>().FirstOrDefault();
							if(resWin == null)
							{
								resWin = new ResolutionSelector();
								resWin.Owner = App.Current.Windows.OfType<OptionWindow>().FirstOrDefault();
								resWin.DataContext = this.ResolutionSelectorViewModel;
								resWin.Show();
							}
							else
							{
								if(resWin.WindowState == WindowState.Minimized)
								{
									resWin.WindowState = WindowState.Normal;
								}
							}
						}
					}
				},
				(param) => 
				{
					Debug.Assert(param != null && (param is int));
					if(param is int si)
					{
						return si<ScreensInfo.Count && si >= 0;
					}
					else return false;
				}
				)
				; }

		public DelegateCommand ResetPreferredResolutionCommand { get =>
				new DelegateCommand(
					(param) =>
					{
						Debug.Assert(param is ScreenInfoModel);
						if(param is ScreenInfoModel targetInfo)
						{
							_selectorTargetDeviceName = targetInfo.DeviceName;
							UpdateScreenResolutionPanel();
						}
					}
					);
		}

		#endregion
	}

	public class ScreenInfoModel
	{
		public static int TotalScreenIndex { get; set; }

		/// <summary>
		/// A 0-based index to distinguish screen order shown. This is also used for judge whether switching screens are available from selector viewmodel
		/// </summary>
		public int ScreenIndex { get; private set; }
		public BitmapSource SampleShot { get; set; }
		public string DeviceName { get; set; }
		public (int width, int height) Resolution { get; set; }
		public double ScaleFactor { get; set; }
		public string ResolutionString { get => $"{Resolution.width}*{Resolution.height} px"; }

		public ScreenInfoModel()
		{
			ScreenIndex = TotalScreenIndex;
			TotalScreenIndex++;
		}
	}
}
