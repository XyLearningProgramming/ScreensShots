﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace ScreenShotWindows.Utils.Coverters
{
	internal class ScreenWindowStatusIsSelectingToColllapseVisibility : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is ScreenShotWindowStatus status)
			{
				if(status == ScreenShotWindowStatus.IsSelecting) return Visibility.Collapsed;
				else return Visibility.Visible;
			}
			else return Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
