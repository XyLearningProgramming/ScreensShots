using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace ScreenShotApp.Utils
{
	public class MultiResBindingsToBooleanConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var res = values[0] as (int width, int height)?;
			var strInfo = values[1] as string;
			if(res != null && res.HasValue)
			{
				return $"{res.Value.width}*{res.Value.height}" == strInfo;
			}
			throw new ArgumentException();
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
