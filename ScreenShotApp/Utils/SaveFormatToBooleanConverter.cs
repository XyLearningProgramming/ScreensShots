using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ScreenShotApp.Utils
{
	public class SaveFormatToBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is SaveFormatStrings preferred && parameter is string buttonString)
			{
				return preferred.ToString() == buttonString;
			}
			throw new ArgumentException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is bool isChecked && parameter is string buttonString)
			{
				if(!isChecked) return Binding.DoNothing;
				return (SaveFormatStrings)Enum.Parse(typeof(SaveFormatStrings), buttonString);
			}
			throw new ArgumentException();
		}
	}
}
