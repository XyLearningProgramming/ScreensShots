using ScreenShotApp.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static ScreenShotApp.ViewModels.OptionsWindowViewModel;

namespace ScreenShotApp
{
	/// <summary>
	/// Interaction logic for OptionWindow.xaml
	/// </summary>
	public partial class OptionWindow : WindowBase
	{
		public OptionWindow()
		{
			InitializeComponent();
		}

		private void Global_KeyChanged(object sender, Controls.KeyChangedEventArgs e)
		{
			if(e.Source is KeyBox kb)
			{
				ShortcutChangeArgs args;
				if(kb.Name == nameof(kb_Capture))
				{
					args = new ShortcutChangeArgs() { e = e, target = ShortcutChangeArgs.ShortcutName.Capture };
					App.Root.OptionsWindowViewModel.ShortcutChangeCommand.Execute(args);
				}
			}
		}

		private void bt_ApplyAll_Click(object sender, RoutedEventArgs e)
		{
			App.Root.OptionsWindowViewModel.ConfirmOptionsCommand.Execute(null);
			this.Close();
		}

		private void bt_Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
