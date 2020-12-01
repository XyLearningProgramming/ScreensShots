using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace ScreenShotApp.Utils
{
	public class MultiBooleansToVisibilityConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			bool flag = true;
			foreach(var o in values)
			{
				if(o is bool bl)
					flag = flag && bl;
				else
				{
					flag = false;
					break;
				}					
			}

			return flag ? Visibility.Visible : Visibility.Collapsed;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
