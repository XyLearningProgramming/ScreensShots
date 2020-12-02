using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ScreenShotApp.Utils
{
	public class DoubleToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is double duration)
			{
				return duration.ToString("0.##");
			}
			throw new ArgumentException(nameof(value));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is string text)
			{
				if(double.TryParse(value as string, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
					return result;
				else return Binding.DoNothing;
			}
			throw new ArgumentException(nameof(value));
		}
	}
}
