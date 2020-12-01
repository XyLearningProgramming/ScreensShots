using ScreenShotWindows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace ScreenShotApp.Utils
{
	class WindowCaptureTargetToBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is WindowsCaptureScreenTarget target && parameter is string buttonParam)
			{
				return (target.ToString() == buttonParam);
			}
			throw new ArgumentException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(parameter is string buttonParam)
				return (bool)value ? Enum.Parse(typeof(WindowsCaptureScreenTarget), buttonParam) : Binding.DoNothing;
			throw new ArgumentException();
		}
	}
}
