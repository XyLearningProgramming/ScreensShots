using ScreenShotWindows.Utils;
using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UserSettingsStruct;

namespace ScreenShotWindows
{
	/// <summary>
	/// Window that captures screen through user32.dll
	/// </summary>
	[TemplatePart(Name = ElementCanvas, Type = typeof(InkCanvas))]
	[TemplatePart(Name = ElementMaskAreaLeft, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementMaskAreaTop, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementMaskAreaRight, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementMaskAreaBottom, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementTargetArea, Type = typeof(InkCanvas))]
	[TemplatePart(Name = ElementMagnifier, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementHintGrid, Type = typeof(GridWithSolidLines))]
	internal class ScreensShotWindows: System.Windows.Window
	{
		#region fields
		private IntPtr _desktopWindowHandler = IntPtr.Zero;

		private ScreenShot _screenShot;
		private WindowsCaptureScreenTarget _screenTarget;
		private WindowsCaptureMode _captureMode;
		private UserSettingsForScreenShotWindows _userSettings;
		private Style _referenceRectStyle;
		private HashSet<Rectangle> _referenceRectsInContainer;
		private bool _saveScreenShots = false;

		private Size _viewBoxSize;
		private VisualBrush _visualPreview;

		private const string ElementCanvas = "PART_Canvas";
		private const string ElementMaskAreaLeft = "PART_MaskAreaLeft";
		private const string ElementMaskAreaTop = "PART_MaskAreaTop";
		private const string ElementMaskAreaRight = "PART_MaskAreaRight";
		private const string ElementMaskAreaBottom = "PART_MaskAreaBottom";
		private const string ElementTargetArea = "PART_TargetArea";
		private const string ElementMagnifier = "PART_Magnifier";
		private const string ElementHintGrid = "PART_HintGrid";
		private readonly string ScreenShotWindowStyleString = "ScreenShotWindowStyle";
		#endregion

		#region properties
		// inkcanvas as main component of this window
		internal InkCanvas Canvas { get; set; }
		// rectangles as mask
		internal FrameworkElement MaskAreaLeft { get; set; }
		internal FrameworkElement MaskAreaTop { get; set; }
		internal FrameworkElement MaskAreaRight { get; set; }
		internal FrameworkElement MaskAreaBottom { get; set; }
		// a boarder element which is target area chosen by mouse dragging
		internal FrameworkElement TargetArea { get; set; }

		internal FrameworkElement Magnifier { get; set; }
		
		internal GridWithSolidLines  HintGrid { get; set; }
		#endregion

		#region dependency properties
		public static readonly DependencyProperty IsSelectingProperty = DependencyProperty.Register("IsSelecting", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(false));
		public bool IsSelecting { get => (bool)GetValue(IsSelectingProperty); set => SetValue(IsSelectingProperty, value); }
		public static readonly DependencyProperty IsDrawingProperty = DependencyProperty.Register("IsDrawing", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(false));
		public bool IsDrawing { get => (bool)GetValue(IsDrawingProperty); set => SetValue(IsDrawingProperty, value); }


		// previewbrush as read-only property for magnifier inner border background
		// it is a reflection of desktop first captured by an inkcanvas component with transparent background
		// then it is stored in this property and sent to magnifier.
		public static readonly DependencyPropertyKey PreviewBrushPropertyKey = DependencyProperty.RegisterReadOnly("PreviewBrush", typeof(Brush), typeof(ScreensShotWindows), new PropertyMetadata(null)); // default value as null
		public static readonly DependencyProperty PreviewBrushProperty = PreviewBrushPropertyKey.DependencyProperty;
		public Brush PreviewBrush { get => (Brush) GetValue(PreviewBrushProperty); private set => SetValue(PreviewBrushPropertyKey, value); }

		//record size of screenshot
		public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(Size), typeof(ScreensShotWindows), new PropertyMetadata(Size.Empty));
		public Size Size { get => (Size)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

		//show what size the selected area is on top left of window
		public static readonly DependencyProperty SizeHintStrProperty = DependencyProperty.Register("SizeHintStr", typeof(string), typeof(ScreensShotWindows), new PropertyMetadata("0 x 0"));
		public string SizeHintStr { get => (string)GetValue(SizeHintStrProperty); set => SetValue(SizeHintStrProperty, value); }

		public static readonly DependencyProperty IsPreviewShowingReferenceLinesProperty = DependencyProperty.Register("IsPreviewShowingReferenceLines", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(false,OnIsShowingReferenceLinesCallback));
		public bool IsPreviewShowingReferenceLines { get => (bool)GetValue(IsPreviewShowingReferenceLinesProperty); set => SetValue(IsPreviewShowingReferenceLinesProperty, value); }

		//disable alt+f4 when screen window popped out. 
		public static readonly DependencyProperty IgnoreAltF4Property = DependencyProperty.Register("IgnoreAltF4", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(false, OnIgnoreAltF4ChangedCallback));
		public bool IgnoreAltF4 { get => (bool)GetValue(IgnoreAltF4Property); set => SetValue(IgnoreAltF4Property, value); }

		//hide window as a standalone in task manager
		public static readonly DependencyProperty ShowInTaskManagerProperty = DependencyProperty.Register("ShowInTaskManager", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(false, OnShowInTaskManagerChangedCallback));
		public bool ShowInTaskManager { get => (bool)GetValue(ShowInTaskManagerProperty); set => SetValue(ShowInTaskManagerProperty, value); }
		#endregion

		#region constructor
		internal ScreensShotWindows(ScreenShot screenShot, WindowsCaptureScreenTarget screenTarget, WindowsCaptureMode captureMode, UserSettingsForScreenShotWindows userSettings)
		{
			//store settings
			_screenShot = screenShot;
			_screenTarget = screenTarget;
			_captureMode = captureMode;
			_userSettings = userSettings;
			
			_referenceRectsInContainer = new HashSet<Rectangle>();
			_referenceRectStyle = Application.Current.TryFindResource("ReferenceDotsDefaultStyle") as Style;
			Debug.Assert(_referenceRectStyle != null);

			//apply transparent style
			Style = Application.Current.TryFindResource("ScreenShotWindowStyle") as Style;
			Debug.Assert(Style != null);
			
			this.DataContext = this;
			this.Loaded += ScreensShotWindowsLoaded;
			this.Closed += ScreenShotWindowsClosed;
		}
		#endregion

		#region methods
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			Canvas = GetTemplateChild(ElementCanvas) as InkCanvas;
			// get a snap shot of canvas and stored as previewBrush. This is projected onto border background. 
			// TODO: change this to fit multiple screens?
			_visualPreview = new VisualBrush(Canvas) {ViewboxUnits = BrushMappingMode.Absolute };
			PreviewBrush = _visualPreview;

			MaskAreaLeft = GetTemplateChild(ElementMaskAreaLeft) as FrameworkElement;
			MaskAreaTop = GetTemplateChild(ElementMaskAreaTop) as FrameworkElement;
			MaskAreaRight = GetTemplateChild(ElementMaskAreaRight) as FrameworkElement;
			MaskAreaBottom = GetTemplateChild(ElementMaskAreaBottom) as FrameworkElement;
			TargetArea = GetTemplateChild(ElementTargetArea) as FrameworkElement;
			Magnifier = GetTemplateChild(ElementMagnifier) as FrameworkElement;
			HintGrid = GetTemplateChild(ElementHintGrid) as GridWithSolidLines;
			GenerateReferenceDotsLines(HintGrid, _userSettings.IsShowingReferenceLine);
			IsPreviewShowingReferenceLines = _userSettings.IsShowingReferenceLine;

			if(Magnifier != null)
			{
				_viewBoxSize = new Size(30, 30);
			}


			Magnifier.Visibility = Visibility.Visible;
		}

		private void ScreensShotWindowsLoaded(object sender, EventArgs args)
		{
			//TODO
		}
		private void ScreenShotWindowsClosed(object sender, EventArgs args)
		{

		}

		/// <summary>
		/// Generate referencec lines inside the designated container depending on usersettings cached
		/// This method is a little bit "unpure" considering it connects with view directly.
		/// But it saved a lot of trouble writing largely repetitive xaml
		/// </summary>
		/// <param name="container"></param>
		private static void OnIsShowingReferenceLinesCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			if(d is ScreensShotWindows window && args.NewValue != args.OldValue)
			{
				foreach(Rectangle rect in window._referenceRectsInContainer)
				{
					window.HintGrid.Children.Remove(rect);
				}
				window._referenceRectsInContainer.Clear();
				window._userSettings.IsShowingReferenceLine = (bool) args.NewValue;
				window.GenerateReferenceDotsLines(window.HintGrid, (bool)args.NewValue);
			}
		}
		private void GenerateReferenceDotsLines(Grid container, bool IsShowingReferenceLine = false)
		{
			Debug.Assert(container != null);
			if(IsShowingReferenceLine)
			{
				// 12 dots + 4 lines
				// *--*--*--*
				// |  |  |  |
				// *--+--+--*
				// |  |  |  |
				// *--+--+--*
				// |  |  |  |
				// *--*--*--*
				for(int col = 0; col < 3; col++)
				{
					// first line
					Rectangle rect = new Rectangle() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
					rect.Style = _referenceRectStyle;
					rect.SetValue(Grid.ColumnProperty, col);
					rect.SetValue(Grid.RowProperty, 0);
					_referenceRectsInContainer.Add(rect);
					container.Children.Add(rect);

					// last line
					Rectangle rect2 = new Rectangle() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };
					rect2.Style = _referenceRectStyle;
					rect2.SetValue(Grid.ColumnProperty, col);
					rect2.SetValue(Grid.RowProperty, 2);
					_referenceRectsInContainer.Add(rect2);
					container.Children.Add(rect2);
					
					// middle 
					if(col==0 || col == 2)
					{
						for(int row = 1; row <= 2; row++)
						{
							Rectangle tmp = new Rectangle() { HorizontalAlignment = col==0?HorizontalAlignment.Left:HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
							tmp.Style = _referenceRectStyle;
							tmp.SetValue(Grid.ColumnProperty, col);
							tmp.SetValue(Grid.RowProperty, row);
							_referenceRectsInContainer.Add(tmp);
							container.Children.Add(tmp);
						}
					}
				}
				// first line
				Rectangle rect3 = new Rectangle() { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
				rect3.Style = _referenceRectStyle;
				rect3.SetValue(Grid.ColumnProperty, 2);
				rect3.SetValue(Grid.RowProperty, 0);
				_referenceRectsInContainer.Add(rect3);
				container.Children.Add(rect3);

				// last line
				Rectangle rect4 = new Rectangle() { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
				rect4.Style = _referenceRectStyle;
				rect4.SetValue(Grid.ColumnProperty, 2);
				rect4.SetValue(Grid.RowProperty, 2);
				_referenceRectsInContainer.Add(rect4);
				container.Children.Add(rect4);
			}
			else
			{
				// 8 dots
				// *--*--*
				// |  |  |
				// *--+--*
				// |  |  |
				// *--*--*
				foreach(HorizontalAlignment horizontalAlignment in new HorizontalAlignment[] {HorizontalAlignment.Left,HorizontalAlignment.Center,HorizontalAlignment.Right })
				{
					foreach(VerticalAlignment verticalAlignment in new VerticalAlignment[] {VerticalAlignment.Top, VerticalAlignment.Center,VerticalAlignment.Bottom })
					{
						if(horizontalAlignment == HorizontalAlignment.Center && verticalAlignment == VerticalAlignment.Center) continue;

						Rectangle rect = new Rectangle() { HorizontalAlignment = horizontalAlignment, VerticalAlignment = verticalAlignment, Margin = new Thickness(horizontalAlignment==HorizontalAlignment.Left?-3:0,verticalAlignment==VerticalAlignment.Top?-3:0,horizontalAlignment==HorizontalAlignment.Right?-3:0,verticalAlignment==VerticalAlignment.Bottom?-3:0) };
						rect.Style = _referenceRectStyle;
						_referenceRectsInContainer.Add(rect);
						container.Children.Add(rect);
					}
				}
			}
		}

		#region interaction by mouse and keyboard
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
		}
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonDown(e);
		}
		protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseDoubleClick(e);
		}
		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			base.OnPreviewMouseMove(e);
		}
		#endregion

		#region Interop operations
		private void UpdateStatus(Point point)
		{

		}
		private void StopHooks()
		{
			MouseHook.Stop();
			MouseHook.StatusChanged -= MouseHook_StatusChanged;
		}
		private void StartHooks()
		{
			MouseHook.Start();
			MouseHook.StatusChanged += MouseHook_StatusChanged;
		}
		private void MouseHook_StatusChanged(object _, MouseHookEventArgs args)
		{

		}
		private void SaveScreenShots()
		{

		}
		private void GetWindowsSnapShots()
		{

		} 
		private BitmapImage GetDesktopWindowSnapShot() {
			_desktopWindowHandler = InteropMethods.GetDesktopWindow_();
			IntPtr _desktopDeviceContext = InteropMethods.GetWindowDC_(_desktopWindowHandler);
		}
		private void MoveElement()
		{

		}
		private void MoveRect()
		{

		}
		private void MoveTargetArea()
		{

		}
		private void MoveMaskArea()
		{

		}
		#endregion

		/// <summary>
		/// Called when IgnoreAltF4 property changed
		/// </summary>
		/// <param name="d"></param>
		/// <param name="args"></param>
		private static void OnIgnoreAltF4ChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			if(d is ScreensShotWindows window)
			{
				if((bool)args.NewValue)
				{
					window.PreviewKeyDown += ScreenWindowPreviewAltF4;
				}
				else
				{
					window.PreviewKeyDown -= ScreenWindowPreviewAltF4;
				}
			}
		}
		private static void ScreenWindowPreviewAltF4(object sender, KeyEventArgs args)
		{
			if(args.Key == Key.System && args.SystemKey == Key.F4)
			{
				args.Handled = true;
			}
		}
		/// <summary>
		/// Called when ShowInTaskManager property changed. If set to false, set the current window's owner as desktop window ("main window").
		/// Notice we first set this in style, which is set above in constructor. Therefore, source initialized will be called at least once afterwards during window start up phase. "source initialized" because we can only get desktop handler from win32 interface.
		/// </summary>
		/// <param name="d"></param>
		/// <param name="args"></param>
		private static void OnShowInTaskManagerChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			if(d is ScreensShotWindows window)
			{
				bool newValue = (bool)args.NewValue;
				window.SetCurrentValue(Window.ShowInTaskbarProperty, newValue);
				if(newValue)
				{
					window.SourceInitialized -= WindowSourceInitialzedCallback;
				}
				else
				{
					window.SourceInitialized += WindowSourceInitialzedCallback;
				}
			}
		}
		//?? is it a must to call right after the SourceInitialized phase ?? why relying on the order of "when style is set"
		private static void WindowSourceInitialzedCallback(object sender, EventArgs args)
		{
			if(sender is ScreensShotWindows window)
			{
				new WindowInteropHelper(window)
				{
					Owner = InteropMethods.GetDesktopWindow_()
				};
			}
		}
		#endregion
	}
}
