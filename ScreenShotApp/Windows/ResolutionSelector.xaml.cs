using ScreenShotApp.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScreenShotApp.Windows
{
	partial class ResolutionSelector: WindowBase
	{
		public ResolutionSelector()
		{
			InitializeComponent();
		}

		private void ExtendedRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
		{
			if(e.OriginalSource is ExtendedRadioButton bt && bt.IsChecked.HasValue && bt.IsChecked.Value)
			{
				App.Root.OptionsWindowViewModel.ResolutionSelectorViewModel.UpdateCurrentScreenInfoCommand.Execute(bt.Text);
			}
		}

		private void IntegerBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			App.Root.OptionsWindowViewModel.ResolutionSelectorViewModel.AddCustomWHInfoCommand.RaiseCanExecuteChanged();
		}
	}
}
