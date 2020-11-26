using ScreenShotApp.Utils;
using ScreenShotApp.MVVMUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ScreenShotApp.Controls;

namespace ScreenShotApp.ViewModels
{
	public class OptionsWindowViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private RootViewModel Root { get; set; }
		public OptionsWindowViewModel(RootViewModel rootViewModel)
		{
			this.Root = rootViewModel;
		}

		// used ONLY in design time
		public OptionsWindowViewModel() { }

		#region public functions
		public void OnWindowLoaded(object sender, RoutedEventArgs args)
		{

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
									CaptureShortcutMainKey = args.e.CurrentKey;
									CaptureShortcutModifierKey = args.e.CurrentModifiers;
								}
							}
						}
					})); 
		}

		private DelegateCommand _confirmOptionsCommand;
		public DelegateCommand ConfirmOptionsCommand
		{
			get => _confirmOptionsCommand ?? (_confirmOptionsCommand = new DelegateCommand(
				(_) => 
				{
					// try update shortcuts
					((App)Application.Current).UpdateShortcutHook();
					Root.StartUpWindowViewModel.CaptureShortcutString = $"{KeyStringHelper.GetSelectKeyText((System.Windows.Input.Key)UserSettingsManager.Instance.CaptureShotcutMainKey, UserSettingsManager.Instance.CaptureShortcutModifierKey, true, true)}";
				}
				));
		}
		#endregion
	}
}
