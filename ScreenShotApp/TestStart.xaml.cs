using ScreenShotApp.MVVMUtils;
using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			//TestApi.StartHooks(MouseHookCallback);
		}

		//protected override void OnClosed(EventArgs e)
		//{
		//	base.OnClosed(e);
		//	TestApi.StopHooks(MouseHookCallback);
		//}

		//private void MouseHookCallback(string info)
		//{
		//	Showing += info;
		//	sv_tb.ScrollToBottom();
		//}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if(e.Key == Key.Escape)
			{
				e.Handled = true;
				Close();
			}
			base.OnPreviewKeyDown(e);
		}
		protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonUp(e);
			Showing += GetCurrentMethod();
			Showing += Environment.NewLine;
		}
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonDown(e);
			Showing += GetCurrentMethod();
			Showing += Environment.NewLine;
		}
		protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseDoubleClick(e);
			Showing += GetCurrentMethod();
			Showing += Environment.NewLine;
			e.Handled = true;
		}
		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			base.OnPreviewMouseMove(e);
			var pt = this.PointToScreen(e.GetPosition(this));
			MousePos = $"Point: {pt.X}*{pt.Y}";
		}

		private static void mouseWaitTimer_Tick(object sender, EventArgs e)
		{

			// Handle Single Click Actions
			Trace.WriteLine("Single Click");
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public string GetCurrentMethod()
		{
			var st = new StackTrace();
			var sf = st.GetFrame(1);

			return sf.GetMethod().Name;
		}
	}
}
