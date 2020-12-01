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

namespace ScreenShotWindows
{
	/// <summary>
	/// Interaction logic for TestWindow.xaml
	/// </summary>
	public partial class StackedButtonsUserControl : UserControl
	{
		public StackedButtonsUserControl()
		{
			InitializeComponent();
		}

		private void PixIntegerBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if(Int32.TryParse(pib_Width.Text, out int intendedWidth) && Int32.TryParse(pib_Height.Text,out int intendedHeight))
			{
				if((this.DataContext as ScreensShotWindows).ScreenShotWindowsCommands.TryChangeTargetAreaSize.CanExecute(null))
				{
					(this.DataContext as ScreensShotWindows).ScreenShotWindowsCommands.TryChangeTargetAreaSize.Execute(new TryChangeTargetAreaCommandParameter() { intendedWidth = intendedWidth, intendedHeight = intendedHeight });
				}
			}

		}
	}
}
