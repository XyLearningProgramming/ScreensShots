using ScreenShotApp.MVVMUtils;
using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScreenShotApp
{
	/// <summary>
	/// Interaction logic for TestStart.xaml
	/// </summary>
	public partial class TestStart : Window, INotifyPropertyChanged
	{
		public TestStart()
		{
			InitializeComponent();
			DataContext = this;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private string showing = "";
		public string Showing { get => showing; set => this.MutateVerbose(ref showing, value, e => this.PropertyChanged?.Invoke(this, e)); }

		private string mousePos = "";
		public string MousePos { get => mousePos; set => this.MutateVerbose(ref mousePos, value, e => this.PropertyChanged?.Invoke(this, e)); }

		private bool isHit = false;
		public bool IsHit { get => isHit; set => this.MutateVerbose(ref isHit, value, e => this.PropertyChanged?.Invoke(this, e)); }

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			//TestApi.StartHooks(MouseHookCallback);
			foreach(var screen in System.Windows.Forms.Screen.AllScreens) 
			{
				sb.Append($"Screen: {screen.Bounds.Left} {screen.Bounds.Top} {screen.Bounds.Right} {screen.Bounds.Bottom}");
				sb.Append(Environment.NewLine);
				//sb.Append($"Is Primary {screen.Primary} BitsperPixel {screen.BitsPerPixel}");
				//sb.Append(Environment.NewLine);
				//sb.Append($"Screen bounds {screen.Bounds.Left} {screen.Bounds.Top} {screen.Bounds.Right} {screen.Bounds.Bottom}");
				//sb.Append(Environment.NewLine);
				uint rawDpi = ScreenInformations.GetDPIFromScreen(screen);
				double factor = rawDpi / (double)96;
				sb.Append($"Raw dpi for this screen is {rawDpi}");
				sb.Append(Environment.NewLine);
				sb.Append($"Adjusted bounds is {screen.Bounds.Left*factor} {screen.Bounds.Top*factor} {screen.Bounds.Right*factor} {screen.Bounds.Bottom*factor}");
				sb.Append(Environment.NewLine);
				sb.Append($"Adjusted Bounds WH is {screen.Bounds.Width * factor} {screen.Bounds.Height * factor} vs {screen.Bounds.Width} {screen.Bounds.Height}");
				sb.Append(Environment.NewLine);
			}
			//sb.Append($"System parameters WH: {SystemParameters.PrimaryScreenWidth} {SystemParameters.PrimaryScreenHeight}");
			//sb.Append(Environment.NewLine);

			//PresentationSource source = PresentationSource.FromVisual(this);

			//double dpiX = 0, dpiY = 0;
			//if(source != null)
			//{
			//	dpiX = source.CompositionTarget.TransformToDevice.M11;
			//	dpiY = source.CompositionTarget.TransformToDevice.M22;
			//}
			//sb.Append($"The scale is {dpiX:0.00}, {dpiY:0.00}");
			//sb.Append(Environment.NewLine);
			sb.Append($"Raw dpi is {ScreenInformations.RawDpi}");
			sb.Append(Environment.NewLine);

			tb_screenInfo.Text = sb.ToString();
		}
	}

	public class ScreenInformations
	{
		public static uint RawDpi { get; private set; }

		static ScreenInformations()
		{
			uint dpiX;
			uint dpiY;
			GetDpi(DpiType.RAW, out dpiX, out dpiY);
			RawDpi = dpiX;
		}

		/// <summary>
		/// Returns the scaling of the given screen.
		/// </summary>
		/// <param name="dpiType">The type of dpi that should be given back..</param>
		/// <param name="dpiX">Gives the horizontal scaling back (in dpi).</param>
		/// <param name="dpiY">Gives the vertical scaling back (in dpi).</param>
		private static void GetDpi(DpiType dpiType, out uint dpiX, out uint dpiY)
		{
			var point = new System.Drawing.Point(1, 1);
			var hmonitor = MonitorFromPoint(point, _MONITOR_DEFAULTTONEAREST);

			switch(GetDpiForMonitor(hmonitor, dpiType, out dpiX, out dpiY).ToInt32())
			{
				case _S_OK: return;
				case _E_INVALIDARG:
					throw new ArgumentException("Unknown error. See https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx for more information.");
				default:
					throw new COMException("Unknown error. See https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx for more information.");
			}
		}

		public static uint GetDPIFromScreen(System.Windows.Forms.Screen screen)
		{
			System.Drawing.Point pt = new System.Drawing.Point(screen.WorkingArea.Left + 1, screen.WorkingArea.Top + 1);
			var hmonitor = MonitorFromPoint(pt, _MONITOR_DEFAULTTONEAREST);
			switch(GetDpiForMonitor(hmonitor, DpiType.RAW, out uint dpiX, out uint dpiY).ToInt32())
			{
				case _S_OK: return dpiX;
				case _E_INVALIDARG:
					throw new ArgumentException("Unknown error. See https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx for more information.");
				default:
					throw new COMException("Unknown error. See https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx for more information.");
			}
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
}
