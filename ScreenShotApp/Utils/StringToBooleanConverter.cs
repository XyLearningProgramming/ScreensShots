using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ScreenShotApp.Utils
{
	public class StringToBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is String valString && parameter is String pmString)
			{
				return valString == pmString;
			}
			throw new ArgumentException(nameof(value));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is Boolean boolVal)
			{
				return boolVal ? parameter : Binding.DoNothing;
			}
			throw new ArgumentException(nameof(value));
		}
	}
}
