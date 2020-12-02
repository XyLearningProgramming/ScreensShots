using ScreenShotApp.MVVMUtils;
using ScreenShotApp.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Media.Imaging;

namespace ScreenShotApp.ViewModels
{
	public class ResolutionSelectorViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		#region constructor
		public ResolutionSelectorViewModel(OptionsWindowViewModel optionsVM)
		{
			OptionsWindowViewModel = optionsVM;
			LoadCustomResolutions();
			LoadBasicScreenResolutions();
		}
		/// <summary>
		/// Use ONLY in design mode
		/// </summary>
		public ResolutionSelectorViewModel() { LoadCustomResolutions(); LoadBasicScreenResolutions(); }
		#endregion

		#region fields&properties
		//public int ScreenIndex { get; set; } = 0;
		//public BitmapSource SnapShotSource { get; set; }
		//public WidthHeightInfoModel WidthHeightInfo { get; set; }
		private ScreenInfoModel _screenInfoModel = new ScreenInfoModel();
		public ScreenInfoModel ScreenInfoModel { get=>_screenInfoModel;
			set
			{ 
				if(this.MutateVerbose(ref _screenInfoModel, value, e => PropertyChanged?.Invoke(this, e)))
				{
					PreviousScreenIndex = value.ScreenIndex - 1;
					NextScreenIndex = value.ScreenIndex + 1;
					OptionsWindowViewModel.ChooseScreenResolutionCommand.RaiseCanExecuteChanged();
				}
			} 
		}

		private int _previousScreenIndex = 0;
		public int PreviousScreenIndex { get => _previousScreenIndex; set=> this.MutateVerbose(ref _previousScreenIndex, value, e => PropertyChanged?.Invoke(this, e)); }
		private int _nextScreenIndex = 0;
		public int NextScreenIndex { get => _nextScreenIndex; set=> this.MutateVerbose(ref _nextScreenIndex, value, e => PropertyChanged?.Invoke(this, e)); }

		public OptionsWindowViewModel OptionsWindowViewModel { get; private set; }
		private (int width, int height) _resolution;
		public (int width, int height) Resolution {get => _resolution;
		set 
			{
				// raise event
				if(this.MutateVerbose(ref _resolution,value, e => PropertyChanged?.Invoke(this, e)))
				{
					// set new prefered resolution
					// grap new picture by resolution
					ScreenInfoModel.Resolution = value;
					UserSettingsManager.Instance.SetPreferredResolution(ScreenInfoModel.DeviceName, ScreenInfoModel);
					OptionsWindowViewModel.ResetPreferredResolutionCommand.Execute(ScreenInfoModel);
				}
			} }
		private int _widthInput = 100;
		public int WidthInput { get => _widthInput; set => this.MutateVerbose(ref _widthInput, value, e => PropertyChanged?.Invoke(this, e)); }
		private int _heightInput = 100;
		public int HeightInput { get => _heightInput; set => this.MutateVerbose(ref _heightInput, value, e => PropertyChanged?.Invoke(this, e)); }

		public int CeilingResInput { get; set; } = 10000;
		public int FloorResInput { get; set; } = 100;

		private ObservableCollection<WidthHeightInfoModel> _customDefinedResolutions;
		public ObservableCollection<WidthHeightInfoModel> CustomDefinedResolutions { get => _customDefinedResolutions; set => _customDefinedResolutions = value; }

		#region constant content
		private ObservableCollection<WidthHeightInfoModel> _fourVThreeResolutions = new ObservableCollection<WidthHeightInfoModel>();
		public ObservableCollection<WidthHeightInfoModel> FourVThreeResolutions { get => _fourVThreeResolutions; set => _fourVThreeResolutions = value; }

		private ObservableCollection<WidthHeightInfoModel> _sixteenVNineResolutions = new ObservableCollection<WidthHeightInfoModel>();
		public ObservableCollection<WidthHeightInfoModel> SixteenVNineResolutions { get => _sixteenVNineResolutions; set => _sixteenVNineResolutions = value; }

		private ObservableCollection<WidthHeightInfoModel> _sixteenVTenResolutions = new ObservableCollection<WidthHeightInfoModel>();
		public ObservableCollection<WidthHeightInfoModel> SixteenVTenResolutions { get => _sixteenVTenResolutions; set => _sixteenVTenResolutions = value; }
		private ObservableCollection<WidthHeightInfoModel> _T21V9Resolutions = new ObservableCollection<WidthHeightInfoModel>();
		public ObservableCollection<WidthHeightInfoModel> T21V9Resolutions { get => _T21V9Resolutions; set => _T21V9Resolutions = value; }

		#endregion
		#endregion
		
		#region methods
		/// <summary>
		/// Load self-defined res, call from constructor
		/// </summary>
		private void LoadCustomResolutions()
		{
			var info = UserSettingsManager.Instance.CustomWHInfo;
			if(info == null || info.Count == 0)
			{
				CustomDefinedResolutions = new ObservableCollection<WidthHeightInfoModel>();
			}
			else
			{
				CustomDefinedResolutions = new ObservableCollection<WidthHeightInfoModel>(info);
			}
		}
		private void LoadBasicScreenResolutions()
		{

			var list = UserSettingsStruct.FrequentResolution.AllResolution["4:3"];
			list.Reverse();
			FourVThreeResolutions.Clear();
			foreach(var pr in list)
			{
				FourVThreeResolutions.Add(new WidthHeightInfoModel() { Height = pr.height, Width = pr.width });
			}
			list = UserSettingsStruct.FrequentResolution.AllResolution["16:9"];
			list.Reverse();
			SixteenVNineResolutions.Clear();
			foreach(var pr in list)
			{
				SixteenVNineResolutions.Add(new WidthHeightInfoModel() { Height = pr.height, Width = pr.width });
			}
			list = UserSettingsStruct.FrequentResolution.AllResolution["16:10"];
			list.Reverse();
			SixteenVTenResolutions.Clear();
			foreach(var pr in list)
			{
				SixteenVTenResolutions.Add(new WidthHeightInfoModel() { Height = pr.height, Width = pr.width });
			}
			list = UserSettingsStruct.FrequentResolution.AllResolution["21:9"];
			list.Reverse();
			T21V9Resolutions.Clear();
			foreach(var pr in list)
			{
				T21V9Resolutions.Add(new WidthHeightInfoModel() { Height = pr.height, Width = pr.width });
			}
		}

		public void UpdateScreenInfo(ScreenInfoModel screenInfoModel)
		{
			//SnapShotSource = screenInfoModel.SampleShot;
			//WidthHeightInfo = new WidthHeightInfoModel() { Width = screenInfoModel.Resolution.width, Height = screenInfoModel.Resolution.height };
			//ScreenIndex = screenInfoModel.ScreenIndex;
			ScreenInfoModel = screenInfoModel;
			Resolution = ScreenInfoModel.Resolution;
			OptionsWindowViewModel.ChooseImagesPathCommand.RaiseCanExecuteChanged();
		}
		#endregion

		#region commands
		public DelegateCommand AddCustomWHInfoCommand { get => new DelegateCommand(
			(param) => 
			{
				WidthHeightInfoModel wh = new WidthHeightInfoModel() { Width = WidthInput, Height = HeightInput };
				UserSettingsManager.Instance.CustomWHInfo.Add(wh);
				if(!CustomDefinedResolutions.Contains(wh))
					CustomDefinedResolutions.Add(wh);
			},
			(param) => 
			{
				return WidthInput >= FloorResInput && WidthInput <= CeilingResInput && HeightInput >= FloorResInput && HeightInput <= CeilingResInput;
			}
			); }

		private DelegateCommand _updateCurrentScreenInfoCommand;
		public DelegateCommand UpdateCurrentScreenInfoCommand {get => _updateCurrentScreenInfoCommand ??
				(_updateCurrentScreenInfoCommand = new DelegateCommand(
					(param) =>
					{
						Debug.Assert(param is string);
						if(param is string str)
						{
							var splitted = str.Split('*');
							this.Resolution = (Convert.ToInt32(splitted[0]), Convert.ToInt32(splitted[1]));
						}
					}
					));
		}
		#endregion
	}

	/// <summary>
	/// Model for displaying one line info of resolution choice
	/// </summary>
	public class WidthHeightInfoModel
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public string InfoString { get => this.ToString(); }

		public override string ToString()
		{
			return $"{Width}*{Height}";
		}
		public override bool Equals(object obj)
		{
			if(obj is WidthHeightInfoModel info_)
				return (Width, Height) == (info_.Width, info_.Height);
			return false;
		}
		public override int GetHashCode()
		{
			return (Width, Height).GetHashCode();
		}
	}
}
