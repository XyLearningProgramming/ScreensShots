using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace UserSettingsStruct
{
	public static class ScreenResolutionInferrer
	{
		private static Dictionary<System.Windows.Forms.Screen, (int width, int height)> _inferredScreenResolution = new Dictionary<System.Windows.Forms.Screen, (int width, int height)>();
		private static Dictionary<System.Windows.Forms.Screen, double> _inferredScales = new Dictionary<System.Windows.Forms.Screen, double>();
		static ScreenResolutionInferrer()
		{
			// infer all screens
			foreach(var screen in System.Windows.Forms.Screen.AllScreens)
			{
				_inferredScreenResolution.Add(screen, InferResolution(screen));
				LogSystemShared.LogWriter.WriteLine($"screen {screen.DeviceName} with inferred width {_inferredScreenResolution[screen].width}, height {_inferredScreenResolution[screen].height}.");
			}
		}

		public static void Initialize() { }

		public static (int width, int height) GetInferredResolution(System.Windows.Forms.Screen screen)
		{
			// search in current dictionary
			if(_inferredScreenResolution.ContainsKey(screen)) return _inferredScreenResolution[screen];
			throw new Exception("Cannot find designated screen");
		}
		public static double GetInferredScale(System.Windows.Forms.Screen screen)
		{
			if(_inferredScales.ContainsKey(screen)) return _inferredScales[screen];
			throw new Exception("Cannot find designated screen"); 
		}

		public static void ForceChangeStoredResolution(System.Windows.Forms.Screen screen, int width, int height)
		{
			if(_inferredScreenResolution.ContainsKey(screen))
			{
				var former = _inferredScreenResolution[screen];
				_inferredScreenResolution[screen] = (width, height);
				_inferredScales[screen] = former.width / (width * 1.0);
				return;
			}
			throw new Exception($"Screen with name {screen.DeviceName} has no presettings. Cannot force set it.");
		}

		private static double GetScaleFactorFromScreen(System.Windows.Forms.Screen screen)
		{
			System.Drawing.Point pt = new System.Drawing.Point(screen.WorkingArea.Left + 1, screen.WorkingArea.Top + 1);
			var hmonitor = MonitorFromPoint(pt, _MONITOR_DEFAULTTONEAREST);
			switch(GetDpiForMonitor(hmonitor, DpiType.RAW, out uint dpiX, out uint dpiY).ToInt32())
			{
				case _S_OK:
					var scale = (int)dpiX / 96.0;
					_inferredScales.Add(screen, scale);
					return scale;
				case _E_INVALIDARG:
					throw new ArgumentException("Unknown error. See https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx for more information.");
				default:
					throw new COMException("Unknown error. See https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx for more information.");
			}
		}

		public static bool IsUsingDifferentDpi(System.Windows.Forms.Screen screen)
		{
			return (screen.Bounds.Width, screen.Bounds.Height) != _inferredScreenResolution[screen];
		}

		private static (int width, int height) InferResolution(System.Windows.Forms.Screen screen)
		{
			//double ratio = screen.Bounds.Width / screen.Bounds.Height;
			//string closestRatio = FrequentResolution.WidthHeightRatios.OrderBy(pr => Math.Abs(ratio - pr.Key)).First().Value;

			double scaleFactor = GetScaleFactorFromScreen(screen);
			double adjustedWidth = screen.Bounds.Width * scaleFactor;
			double adjustedHeight = screen.Bounds.Height * scaleFactor;
			LogSystemShared.LogWriter.WriteLine($"screen {screen.DeviceName} original width {screen.Bounds.Width} height {screen.Bounds.Height}");
			LogSystemShared.LogWriter.WriteLine($"screen {screen.DeviceName} adjusted width {adjustedWidth:0.00} height {adjustedHeight:0.00}");
			LogSystemShared.LogWriter.WriteLine($"screen {screen.DeviceName} scale factor {scaleFactor:0.00}");
			return FrequentResolution.AllResolution.SelectMany(pr=>pr.Value).OrderBy(tp => (Math.Abs(adjustedWidth - tp.width) + Math.Abs(adjustedHeight - tp.height))).First(); //manhattan distacne because it's simpler
		}

		//https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062.aspx
		[DllImport("User32.dll")]
		private static extern IntPtr MonitorFromPoint([In] System.Drawing.Point pt, [In] uint dwFlags);

		//https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx
		[DllImport("Shcore.dll")]
		private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

		const int _S_OK = 0;
		const int _MONITOR_DEFAULTTONEAREST = 2;
		const int _E_INVALIDARG = -2147024809;
	}

	/// <summary>
	/// Represents the different types of scaling.
	/// </summary>
	/// <seealso cref="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511.aspx"/>
	public enum DpiType
	{
		EFFECTIVE = 0,
		ANGULAR = 1,
		RAW = 2,
	}

	public static class FrequentResolution 
	{
		public static Dictionary<string, List<(int width, int height)>> AllResolution = new Dictionary<string, List<(int width, int height)>>()
		{
			["4:3"] = new List<(int width, int height)>() { (640, 480), (800, 600), (960, 720), (1024, 768), (1280, 960), (1400, 1050), (1440, 1080), (1600, 1200), (1856, 1392), (1920, 1440), (2048, 1536) },
			["16:10"] = new List<(int width, int height)>() { (1280,800), (1440,900), (1680,1050), (1920,1200), (2560,1600) },
			["16:9"] = new List<(int width, int height)>() { (1024,576), (1152,648), (1280,720), (1366,768), (1600,900), (1920,1080), (2560,1440), (3840,2160), (7680, 4320) },
			["21:9"] = new List<(int width, int height)>() { (2560,1080), (3440,1440), (5120,2160) }
		};

		public static Dictionary<double, string> WidthHeightRatios = new Dictionary<double, string>
		{
			[4 / 3.0] = "4:3",
			[16 / 10.0] = "16:10",
			[16 / 9.0] = "16:9",
			[21 / 9.0] = "21:9",
		};
	}
}
