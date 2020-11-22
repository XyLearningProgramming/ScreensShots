using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ScreenShotWindows.Utils.Coverters
{
	internal class ScreenWindowStatusEmptyToFalse : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is ScreenShotWindowStatus status)
			{
				if(status == ScreenShotWindowStatus.Empty) return false;
				else return true;
			}
			else return true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
