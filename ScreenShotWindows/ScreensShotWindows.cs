using ScreenShotWindows.Utils;
using ScreenShotWindows.Utils.Interop;
using LogSystemShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
	public enum ScreenShotWindowStatus {Empty,IsDrawing,IsSelecting }
	public enum TargetAreaSelectingStatus {Empty,Dragging,Resizing }

	/// <summary>
	/// Window that captures screen through user32.dll
	/// </summary>
	[TemplatePart(Name = ElementCanvas, Type = typeof(InkCanvas))]
	[TemplatePart(Name = ElementMaskAreaLeft, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementMaskAreaTop, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementMaskAreaRight, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementMaskAreaBottom, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementStackedButtons, Type = typeof(StackPanel))]
	[TemplatePart(Name = ElementTargetArea, Type = typeof(InkCanvas))]
	[TemplatePart(Name = ElementMagnifier, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = ElementHintGrid, Type = typeof(GridWithSolidLines))]
	internal class ScreensShotWindows : System.Windows.Window
	{
		#region fields
		private static readonly Guid BmpGuid = new Guid("{b96b3cab-0728-11d3-9d7b-0000f81ef32e}"); // do not change a bit

		private IntPtr _desktopWindowHandler = IntPtr.Zero;
		private InteropStructs.RECT _desktopWindowRect;

		private ScreenShot _screenShot;

		private BitmapImage _desktopSnapShot;
		private WindowsCaptureScreenTarget _screenTarget;
		private WindowsCaptureMode _captureMode;
		private UserSettingsForScreenShotWindows _userSettings;
		private Style _referenceRectStyle;
		private HashSet<Rectangle> _referenceRectsInContainer;
		private bool _saveScreenShots = false;

		private Point _drawingMouseStartPosition = new Point(0,0);
		private Point _selectingMouseStartPosition = new Point(0, 0);
		private Point _selectingMousePressedLastFramePosition = new Point(0, 0);
		private TargetAreaSelectingStatus targetAreaSelectingStatus = TargetAreaSelectingStatus.Empty;

		private const int _mouseMoveLegalThreshold = 3;
		private const int _mouseCursorHitThreshold = 7;

		private const string ElementCanvas = "PART_Canvas";
		private const string ElementMaskAreaLeft = "PART_MaskAreaLeft";
		private const string ElementMaskAreaTop = "PART_MaskAreaTop";
		private const string ElementMaskAreaRight = "PART_MaskAreaRight";
		private const string ElementMaskAreaBottom = "PART_MaskAreaBottom";
		private const string ElementTargetArea = "PART_TargetArea";
		private const string ElementMagnifier = "PART_Magnifier";
		private const string ElementHintGrid = "PART_HintGrid";
		private const string ElementStackedButtons = "PART_StackedButtons";
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

		internal StackPanel StackedButtons { get; set; }

		internal GridWithSolidLines HintGrid { get; set; }


		public ScreenShotWindowsCommands ScreenShotWindowsCommands { get; set; }
		public IntPtr DesktopWindowHandler { get => _desktopWindowHandler; }
		public InteropStructs.RECT DesktopWindowRect { get => _desktopWindowRect; }
		public VisualBrush MagnifierPreviewVisualBrush { get; private set; }
		public InteropStructs.POINT MagnifierOffset { get; private set; } = new InteropStructs.POINT(5, 25);
		public List<Tuple<IntPtr, InteropStructs.RECT>> AllWinHandlers { get; set; } = new List<Tuple<IntPtr, InteropStructs.RECT>>();

		public InteropStructs.RECT TargetAreaRECT {get=> new InteropStructs.RECT((int)TargetArea.Margin.Left, (int)TargetArea.Margin.Top, (int)TargetArea.Margin.Left + (int)TargetArea.Width, (int)TargetArea.Margin.Top + (int)TargetArea.Height); }
		#endregion

		#region dependency properties
		//public static readonly DependencyProperty IsSelectingProperty = DependencyProperty.Register("IsSelecting", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(false));
		//public bool IsSelecting { get => (bool)GetValue(IsSelectingProperty); set => SetValue(IsSelectingProperty, value); }
		//public static readonly DependencyProperty IsDrawingProperty = DependencyProperty.Register("IsDrawing", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(false));
		//public bool IsDrawing { get => (bool)GetValue(IsDrawingProperty); set => SetValue(IsDrawingProperty, value); }
		public static readonly DependencyPropertyKey WindowStatusPropertyKey = DependencyProperty.RegisterReadOnly("WindowStatus", typeof(ScreenShotWindowStatus), typeof(ScreensShotWindows), new PropertyMetadata(ScreenShotWindowStatus.Empty, ScreenShotWindowStatusChangedCallback)); // default value as null
		public static readonly DependencyProperty WindowStatusProperty = WindowStatusPropertyKey.DependencyProperty;
		public ScreenShotWindowStatus WindowStatus { get => (ScreenShotWindowStatus)GetValue(WindowStatusProperty); set => SetValue(WindowStatusPropertyKey, value); }

		// previewbrush as read-only property for magnifier inner border background
		// it is a reflection of desktop first captured by an inkcanvas component with transparent background
		// then it is stored in this property and sent to magnifier.
		public static readonly DependencyPropertyKey PreviewBrushPropertyKey = DependencyProperty.RegisterReadOnly("PreviewBrush", typeof(Brush), typeof(ScreensShotWindows), new PropertyMetadata(null)); // default value as null
		public static readonly DependencyProperty PreviewBrushProperty = PreviewBrushPropertyKey.DependencyProperty;
		public Brush PreviewBrush { get => (Brush)GetValue(PreviewBrushProperty); private set => SetValue(PreviewBrushPropertyKey, value); }

		//record size of screenshot
		public static readonly DependencyProperty TargetAreaSizeProperty = DependencyProperty.Register("TargetAreaSize", typeof(Size), typeof(ScreensShotWindows), new PropertyMetadata(Size.Empty));
		public Size TargetAreaSize { get => (Size)GetValue(TargetAreaSizeProperty); set => SetValue(TargetAreaSizeProperty, value); }

		//magnifier size
		public static readonly DependencyProperty MagnifierViewBoxSizeProperty = DependencyProperty.Register("MagnifierViewBoxSize", typeof(Size), typeof(ScreensShotWindows), new PropertyMetadata(Size.Empty));
		public Size MagnifierViewBoxSize { get => (Size)GetValue(MagnifierViewBoxSizeProperty); set => SetValue(MagnifierViewBoxSizeProperty, value); }

		////show what size the selected area is on top left of window
		//public static readonly DependencyProperty SizeHintStrProperty = DependencyProperty.Register("SizeHintStr", typeof(string), typeof(ScreensShotWindows), new PropertyMetadata("0 x 0"));
		//public string SizeHintStr { get => (string)GetValue(SizeHintStrProperty); set => SetValue(SizeHintStrProperty, value); } 
		public static readonly DependencyPropertyKey MagnifierHintTextPropertyKey = DependencyProperty.RegisterReadOnly("MagnifierHintText", typeof(string), typeof(ScreensShotWindows), new PropertyMetadata("POS: (1000,1000) RGB: (255,255,255)"));
		public static readonly DependencyProperty MagnifierHintTextProperty = MagnifierHintTextPropertyKey.DependencyProperty;
		public string MagnifierHintText { get => (string)GetValue(MagnifierHintTextProperty); set => SetValue(MagnifierHintTextPropertyKey, value); }

		public static readonly DependencyProperty IsPreviewShowingReferenceLinesProperty = DependencyProperty.Register("IsPreviewShowingReferenceLines", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(false, OnIsShowingReferenceLinesCallback));
		public bool IsPreviewShowingReferenceLines { get => (bool)GetValue(IsPreviewShowingReferenceLinesProperty); set => SetValue(IsPreviewShowingReferenceLinesProperty, value); }

		//disable alt+f4 when screen window popped out. 
		public static readonly DependencyProperty IgnoreAltF4Property = DependencyProperty.Register("IgnoreAltF4", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(false, OnIgnoreAltF4ChangedCallback));
		public bool IgnoreAltF4 { get => (bool)GetValue(IgnoreAltF4Property); set => SetValue(IgnoreAltF4Property, value); }

		//hide window as a standalone in task manager
		public static readonly DependencyProperty ShowInTaskManagerProperty = DependencyProperty.Register("ShowInTaskManager", typeof(bool), typeof(ScreensShotWindows), new PropertyMetadata(true, OnShowInTaskManagerChangedCallback));
		public bool ShowInTaskManager { get => (bool)GetValue(ShowInTaskManagerProperty); set => SetValue(ShowInTaskManagerProperty, value); }
		#endregion

		#region constructor
		internal ScreensShotWindows(ScreenShot screenShot, WindowsCaptureScreenTarget screenTarget, WindowsCaptureMode captureMode, UserSettingsForScreenShotWindows userSettings)
		{
			this.Init(screenShot, screenTarget, captureMode, userSettings);
		}

		internal ScreensShotWindows(System.Windows.Forms.Screen targetScreen, ScreenShot screenShot, WindowsCaptureScreenTarget screenTarget, WindowsCaptureMode captureMode, UserSettingsForScreenShotWindows userSettings)
		{
			this.WindowStartupLocation = WindowStartupLocation.Manual;
			this.Left = targetScreen.Bounds.Left;
			this.Top = targetScreen.Bounds.Top;
			this.Width = targetScreen.Bounds.Width;
			this.Height = targetScreen.Bounds.Height;
			this.Init(screenShot, screenTarget, captureMode, userSettings);
		}

		internal void Init(ScreenShot screenShot, WindowsCaptureScreenTarget screenTarget, WindowsCaptureMode captureMode, UserSettingsForScreenShotWindows userSettings)
		{
			//store settings
			_screenShot = screenShot;
			_screenTarget = screenTarget;
			_captureMode = captureMode;
			_userSettings = userSettings;

			//init command class
			ScreenShotWindowsCommands = new ScreenShotWindowsCommands(this);

			//apply dot style
			_referenceRectsInContainer = new HashSet<Rectangle>();
			_referenceRectStyle = Application.Current.TryFindResource("ReferenceDotsDefaultStyle") as Style;
			Debug.Assert(_referenceRectStyle != null);

			//apply transparent style
			Style = Application.Current.TryFindResource("ScreenShotWindowStyle") as Style;
			Debug.Assert(Style != null);

			this.DataContext = this;
			this.Loaded += ScreensShotWindowsLoaded;
			this.Closed += ScreenShotWindowsClosed;

			// cache all window handlers
			InteropMethods.EnumWindows_((IntPtr wnd, IntPtr param) =>
			{
				IntPtr thisWindowHandler = new WindowInteropHelper(this).Handle;
				if(InteropMethods.IsWindowVisible_(wnd) && InteropMethods.IsWindowEnabled_(wnd) && wnd != thisWindowHandler)
				{
					InteropMethods.GetWindowRect_(wnd, out InteropStructs.RECT rect);
					AllWinHandlers.Add(new Tuple<IntPtr, InteropStructs.RECT>(wnd, rect));
				}
				return true;
			}, IntPtr.Zero);
		}
		#endregion

		#region methods
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			Canvas = GetTemplateChild(ElementCanvas) as InkCanvas;
			// get a snap shot of canvas and stored as previewBrush. Later bind onto magnifier
			MagnifierPreviewVisualBrush = new VisualBrush(Canvas) { ViewboxUnits = BrushMappingMode.Absolute };
			PreviewBrush = MagnifierPreviewVisualBrush;

			MaskAreaLeft = GetTemplateChild(ElementMaskAreaLeft) as FrameworkElement;
			MaskAreaTop = GetTemplateChild(ElementMaskAreaTop) as FrameworkElement;
			MaskAreaRight = GetTemplateChild(ElementMaskAreaRight) as FrameworkElement;
			MaskAreaBottom = GetTemplateChild(ElementMaskAreaBottom) as FrameworkElement;
			TargetArea = GetTemplateChild(ElementTargetArea) as FrameworkElement;
			Magnifier = GetTemplateChild(ElementMagnifier) as FrameworkElement;
			StackedButtons = GetTemplateChild(ElementStackedButtons) as StackPanel;
			HintGrid = GetTemplateChild(ElementHintGrid) as GridWithSolidLines;
			GenerateReferenceDotsLines(HintGrid, _userSettings.IsShowingReferenceLine);
			IsPreviewShowingReferenceLines = _userSettings.IsShowingReferenceLine;

			if(Magnifier != null)
			{
				MagnifierViewBoxSize = new Size(30, 30); // 121 121
			}

		}

		private void ScreensShotWindowsLoaded(object sender, EventArgs args)
		{
			if(_screenTarget == WindowsCaptureScreenTarget.MainScreen)
			{
				_desktopSnapShot = GetCurrentWindowSnapShot();
				LogWriter.WriteLine("Desktop screen shot captured.");
				Image image = new Image() { Source = _desktopSnapShot };
				RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
				Canvas.Children.Add(image); // canvas is transparent now

				//_desktopSnapShot.SaveToLocal();
			}

			// move target area and magnifier
			InteropStructs.POINT mousePoint = new InteropStructs.POINT(Mouse.GetPosition(this));
			MoveCommandParameter param = new MoveCommandParameter() { targetElement = TargetArea, targetPoint = mousePoint };
			if(ScreenShotWindowsCommands.MoveCommand.CanExecute(param))
			{
				ScreenShotWindowsCommands.MoveCommand.Execute(param);
			}
			// magnifier param settings
			MoveMagnifierCommandParameter magParam = new MoveMagnifierCommandParameter() { targetElement = TargetArea, targetPoint = mousePoint, Offset = MagnifierOffset, ViewboxSize = MagnifierViewBoxSize };
			if(ScreenShotWindowsCommands.MoveCommand.CanExecute(magParam))
			{
				ScreenShotWindowsCommands.MoveCommand.Execute(magParam);
			}

			// mouse start to respond 
			StartHooks();
			// force to focus window to receive mouse event on
			this.Focus();
		}
		private void ScreenShotWindowsClosed(object sender, EventArgs args)
		{
			StopHooks();
			_screenShot.OnClose(); // close callback

			if(_saveScreenShots)
			{
				InteropStructs.RECT rect = TargetAreaRECT;
				CroppedBitmap image = new CroppedBitmap(_desktopSnapShot, new Int32Rect(rect.Left, rect.Top, rect.Width, rect.Height));
				image.SaveToLocal();
			}

			this.Loaded -= ScreensShotWindowsLoaded;
			this.Closed -= ScreenShotWindowsClosed;
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
				window._userSettings.IsShowingReferenceLine = (bool)args.NewValue;
				window.GenerateReferenceDotsLines(window.HintGrid, (bool)args.NewValue);
			}
		}

		private static void ScreenShotWindowStatusChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			if(d is ScreensShotWindows window && args.NewValue != args.OldValue)
			{
				if(args.OldValue is ScreenShotWindowStatus OldStatus && args.NewValue is ScreenShotWindowStatus NewStatus)
				{
					if(OldStatus == ScreenShotWindowStatus.IsDrawing && NewStatus == ScreenShotWindowStatus.IsSelecting)
					{
						window._screenShot.OnTargetAreaSelected(window);
					}
				}
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
					Debug.Assert(_referenceRectStyle!=null);
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
					if(col == 0 || col == 2)
					{
						for(int row = 1; row <= 2; row++)
						{
							Rectangle tmp = new Rectangle() { HorizontalAlignment = col == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
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
				foreach(HorizontalAlignment horizontalAlignment in new HorizontalAlignment[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
				{
					foreach(VerticalAlignment verticalAlignment in new VerticalAlignment[] { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom })
					{
						if(horizontalAlignment == HorizontalAlignment.Center && verticalAlignment == VerticalAlignment.Center) continue;

						Rectangle rect = new Rectangle() { HorizontalAlignment = horizontalAlignment, VerticalAlignment = verticalAlignment, Margin = new Thickness(horizontalAlignment == HorizontalAlignment.Left ? -3 : 0, verticalAlignment == VerticalAlignment.Top ? -3 : 0, horizontalAlignment == HorizontalAlignment.Right ? -3 : 0, verticalAlignment == VerticalAlignment.Bottom ? -3 : 0) };
						// set grid 
						if(verticalAlignment == VerticalAlignment.Center)
							Grid.SetRow(rect, 1);
						if(verticalAlignment == VerticalAlignment.Bottom)
							Grid.SetRow(rect, 2);
						if(horizontalAlignment == HorizontalAlignment.Center)
							Grid.SetColumn(rect, 1);
						if(horizontalAlignment == HorizontalAlignment.Right)
							Grid.SetColumn(rect, 2);
						rect.Style = _referenceRectStyle;
						_referenceRectsInContainer.Add(rect);
						container.Children.Add(rect);
					}
				}
			}
			container.InvalidateVisual();
			LogWriter.WriteLine($"Generated dots: {_referenceRectsInContainer.Count}");
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
			if(WindowStatus == ScreenShotWindowStatus.IsDrawing)
			{
				WindowStatus = ScreenShotWindowStatus.IsSelecting;
				e.Handled = true;
			}else if(WindowStatus == ScreenShotWindowStatus.IsSelecting)
			{
				if(targetAreaSelectingStatus != TargetAreaSelectingStatus.Empty)
				{
					targetAreaSelectingStatus = TargetAreaSelectingStatus.Empty;
					ScreenShotWindowsCommands.UpdateMouseCursorCommand.TryExecute(new UpdateMouseCurosrCommandParameter() { MouseLocalPoint = new InteropStructs.POINT(e.GetPosition(this)), SnapLength = _mouseCursorHitThreshold });
				}
				e.Handled = true;
			}
		}
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonDown(e);
			if(WindowStatus == ScreenShotWindowStatus.Empty)
			{
				WindowStatus = ScreenShotWindowStatus.IsDrawing;
				_drawingMouseStartPosition = e.GetPosition(this);
				e.Handled = true;
				return;
			}
			else if(WindowStatus == ScreenShotWindowStatus.IsSelecting)
			{
				// respond to selecting drag events
				_selectingMouseStartPosition = e.GetPosition(this);
				_selectingMousePressedLastFramePosition = e.GetPosition(this);
				e.Handled = true;
				return;
			}
		}
		protected override void OnPreviewMouseDoubleClick(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseDoubleClick(e);
			if(e.ChangedButton == MouseButton.Left)
			{
				if(WindowStatus == ScreenShotWindowStatus.Empty)
				{
					WindowStatus = ScreenShotWindowStatus.IsSelecting; // select the snapped window
					e.Handled = true;
					return;
				}
				else if(WindowStatus == ScreenShotWindowStatus.IsSelecting)
				{
					_saveScreenShots = true;
					this.Close();
					e.Handled = true;
					return;
				}
			}
			else if(e.ChangedButton == MouseButton.Right)
			{
				// double right click means quit
				if(WindowStatus != ScreenShotWindowStatus.IsDrawing)
				{
					this.Close();
					e.Handled = true;
					return;
				}
			}
		}

		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			base.OnPreviewMouseMove(e);
			if(WindowStatus == ScreenShotWindowStatus.Empty)
			{
				// snap target area & move magnifier
				MoveCommandParameter targetAreaMoveParam = new MoveCommandParameter() { targetElement=TargetArea, targetPoint = new InteropStructs.POINT(this.PointToScreen(e.GetPosition(this))) };
				ScreenShotWindowsCommands.MoveCommand.TryExecute(targetAreaMoveParam);
				MoveMagnifierCommandParameter magnifierParam = new MoveMagnifierCommandParameter() { targetElement = Magnifier, targetPoint = targetAreaMoveParam.targetPoint, ViewboxSize = this.MagnifierViewBoxSize, Offset = MagnifierOffset };
				ScreenShotWindowsCommands.MoveCommand.TryExecute(magnifierParam);

				e.Handled = true;
			}
			else if(WindowStatus == ScreenShotWindowStatus.IsDrawing && e.LeftButton== MouseButtonState.Pressed)
			{
				Point mousePos = e.GetPosition(this);
				if(IsMouseDeltaTooSmall(mousePos, _drawingMouseStartPosition)) return;
				// update target area 
				InteropStructs.RECT rect = new InteropStructs.RECT(0,0,0,0);
				rect.Left = Math.Min((int)_drawingMouseStartPosition.X, (int)mousePos.X);
				rect.Top = Math.Min((int)_drawingMouseStartPosition.Y, (int)mousePos.Y);
				rect.Right = Math.Max((int)_drawingMouseStartPosition.X, (int)mousePos.X);
				rect.Bottom = Math.Max((int)_drawingMouseStartPosition.Y, (int)mousePos.Y);

				ReSizeTargetAreaCommandParameter reparam = new ReSizeTargetAreaCommandParameter() { TargetRect = rect };
				ScreenShotWindowsCommands.ResizeTargetAreaCommand.TryExecute(reparam);

				MoveMagnifierCommandParameter magnifierParam = new MoveMagnifierCommandParameter() { targetElement = Magnifier, targetPoint = new InteropStructs.POINT(mousePos), ViewboxSize = this.MagnifierViewBoxSize, Offset = MagnifierOffset };
				ScreenShotWindowsCommands.MoveCommand.TryExecute(magnifierParam);

				e.Handled = true;
			}
			else if(WindowStatus== ScreenShotWindowStatus.IsSelecting)
			{
				Point mousePos = e.GetPosition(this);
				if(targetAreaSelectingStatus == TargetAreaSelectingStatus.Empty)
					ScreenShotWindowsCommands.UpdateMouseCursorCommand.TryExecute(new UpdateMouseCurosrCommandParameter() { MouseLocalPoint = new InteropStructs.POINT(mousePos), SnapLength = _mouseCursorHitThreshold });

				if(e.LeftButton == MouseButtonState.Pressed)
				{
					// two behavior: drag & resize
					// it's ugly to use cursor icon, but it's the most efficient.
					if(targetAreaSelectingStatus == TargetAreaSelectingStatus.Empty && this.Cursor == Cursors.Arrow) { e.Handled = true; return; }
					// mouse move if inside target area that means drag
					InteropStructs.RECT rect = TargetAreaRECT;
					if(targetAreaSelectingStatus == TargetAreaSelectingStatus.Dragging || (targetAreaSelectingStatus == TargetAreaSelectingStatus.Empty && this.Cursor == Cursors.SizeAll))
					{
						targetAreaSelectingStatus = TargetAreaSelectingStatus.Dragging;
						// drag 
						Vector distance = mousePos - _selectingMousePressedLastFramePosition;
						ReSizeTargetAreaCommandParameter reparam = new ReSizeTargetAreaCommandParameter() { TargetRect = rect.Translation(new InteropStructs.POINT((int)distance.X, (int)distance.Y)) };
						ScreenShotWindowsCommands.ResizeTargetAreaCommand.TryExecute(reparam);
					}
					else if((targetAreaSelectingStatus == TargetAreaSelectingStatus.Resizing) || (targetAreaSelectingStatus == TargetAreaSelectingStatus.Empty && this.Cursor != Cursors.SizeAll && this.Cursor!=Cursors.Arrow))
					{
						targetAreaSelectingStatus = TargetAreaSelectingStatus.Resizing;
						InteropStructs.RECT newRect = rect;
						InteropStructs.POINT pt = new InteropStructs.POINT(mousePos);
						// enlarge 
						bool leftAbs = Math.Abs(_selectingMouseStartPosition.X - rect.Left) <= _mouseCursorHitThreshold;
						bool topAbs = Math.Abs(_selectingMouseStartPosition.Y - rect.Top) <= _mouseCursorHitThreshold;
						bool rightAbs = Math.Abs(_selectingMouseStartPosition.X - rect.Right) <= _mouseCursorHitThreshold;
						bool downAbs = Math.Abs(_selectingMouseStartPosition.Y - rect.Bottom) <= _mouseCursorHitThreshold;
						if(leftAbs)
						{
							newRect.Left = pt.X;
						}
						else if(rightAbs)
						{
							newRect.Right = pt.X;
						}
						if(topAbs)
						{
							newRect.Top = pt.Y;
						}
						else if(downAbs)
						{
							newRect.Bottom = pt.Y;
						}
						ReSizeTargetAreaCommandParameter reparam = new ReSizeTargetAreaCommandParameter() { TargetRect = newRect };
						ScreenShotWindowsCommands.ResizeTargetAreaCommand.TryExecute(reparam);
					}
					MoveMagnifierCommandParameter magnifierParam = new MoveMagnifierCommandParameter() { targetElement = Magnifier, targetPoint = new InteropStructs.POINT(mousePos), ViewboxSize = this.MagnifierViewBoxSize, Offset = MagnifierOffset };
					ScreenShotWindowsCommands.MoveCommand.TryExecute(magnifierParam);

					_selectingMousePressedLastFramePosition = mousePos;
					TargetArea.InvalidateVisual();
				}
				e.Handled = true;

			}
		}

		private bool IsMouseDeltaTooSmall(Point p1, Point p2)
		{
			Vector distance = p1 - p2;
			if(Math.Abs(distance.X) <= _mouseMoveLegalThreshold && Math.Abs(distance.Y) <= _mouseMoveLegalThreshold)
			{
				// too small move not update anything 
				return true;
			}
			else return false;
		}

		protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseRightButtonUp(e);
			if(WindowStatus== ScreenShotWindowStatus.Empty)
			{
				this.Close();
				e.Handled = true;
			}
			else if(WindowStatus == ScreenShotWindowStatus.IsSelecting)
			{
				WindowStatus = ScreenShotWindowStatus.Empty;
				// snap target area & move magnifier
				MoveCommandParameter targetAreaMoveParam = new MoveCommandParameter() { targetElement = TargetArea, targetPoint = new InteropStructs.POINT(this.PointToScreen(e.GetPosition(this))) };
				ScreenShotWindowsCommands.MoveCommand.TryExecute(targetAreaMoveParam);
				MoveMagnifierCommandParameter magnifierParam = new MoveMagnifierCommandParameter() { targetElement = Magnifier, targetPoint = targetAreaMoveParam.targetPoint, ViewboxSize = this.MagnifierViewBoxSize, Offset = MagnifierOffset };
				ScreenShotWindowsCommands.MoveCommand.TryExecute(magnifierParam);
				e.Handled = true;
			}
		}
		#endregion

		#region Interop operations
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
			//respond to mouse click events here. Change main status.
			// can I do this without a global hook?
		}
		private void GetWindowsSnapShots()
		{

		}
		private BitmapImage GetDesktopWindowSnapShot()
		{
			_desktopWindowHandler = InteropMethods.GetDesktopWindow_();
			return GetWindowSnapShot(_desktopWindowHandler);
		}

		private BitmapImage GetForegroundScreenSnapShot()
		{
			_desktopWindowHandler = InteropMethods.GetForegroundWindow_();
			return GetWindowSnapShot(_desktopWindowHandler);
		}
		private BitmapImage GetCurrentWindowSnapShot()
		{
			return GetWindowSnapShot(new WindowInteropHelper(this).Handle);
		}
		private BitmapImage GetWindowSnapShot(IntPtr windowHandler)
		{
			try
			{
				var hdcSrc = InteropMethods.GetWindowDC_(windowHandler);
				var hdcDest = InteropMethods.CreateCompatibleDC_(hdcSrc);

				InteropMethods.GetWindowRect_(windowHandler, out _desktopWindowRect);
				LogWriter.WriteLine($"Captured window size is: {_desktopWindowRect.Width}*{_desktopWindowRect.Height}");
				//InteropMethods.GetWindowInfo_(windowHandler, out InteropStructs.WINDOWINFO windowInfo);
				//_desktopWindowRect = windowInfo.rcWindow;
				//LogWriter.WriteLine($"Captured window size from window info call is: {_desktopWindowRect.Width}*{_desktopWindowRect.Height}");
				//LogWriter.WriteLine($"Captured window (client) size from window info call is: {windowInfo.rcClient.Width}*{windowInfo.rcClient.Height}");

				var hbitmap = InteropMethods.CreateCompatibleBitmap_(hdcSrc, _desktopWindowRect.Width, _desktopWindowRect.Height);


				var hOld = InteropMethods.SelectObject_(hdcDest, hbitmap);
				InteropMethods.BitBlt_(hdcDest, 0, 0, _desktopWindowRect.Width, _desktopWindowRect.Height, hdcSrc, 0, 0, InteropConstants.SRCCOPY);
				InteropMethods.SelectObject_(hdcDest, hOld);
				InteropMethods.DeleteDC_(hdcDest);
				InteropMethods.ReleaseDC_(windowHandler, hdcSrc);

				InteropMethods.Gdip.GdipCreateBitmapFromHBITMAP_(new HandleRef(null, hbitmap), new HandleRef(null, IntPtr.Zero), out var bitmap).GdipExceptionHandler();

				using var ms = new MemoryStream();
				InteropMethods.Gdip.GdipGetImageEncodersSize_(out var numEncoders, out var size).GdipExceptionHandler();

				var memory = Marshal.AllocHGlobal(size);
				InteropMethods.Gdip.GdipGetImageEncoders_(numEncoders, size, memory).GdipExceptionHandler();

				var codecInfo = ImageCodecInfo.ConvertFromMemory(memory, numEncoders).FirstOrDefault(item => item.FormatID.Equals(BmpGuid));
				if(codecInfo == null) throw new Exception("ImageCodecInfo is null");

				var encoderParamsMemory = IntPtr.Zero;

				var g = codecInfo.Clsid;
				InteropMethods.Gdip.GdipSaveImageToStream_(new HandleRef(this, bitmap),
					new InteropStructs.ComStreamFromDataStream(ms), ref g,
					new HandleRef(null, encoderParamsMemory)).GdipExceptionHandler();
				if(encoderParamsMemory != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(encoderParamsMemory);
				}

				Marshal.FreeHGlobal(memory);

				var bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.StreamSource = ms;
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				return bitmapImage;
			}
			catch(Exception e)
			{
				LogWriter.WriteLine(e.Message, "Fatal error");
				Application.Current.Shutdown();
				return null;
			}
		}

		/// <summary>
		/// Resolve target area should-be position
		/// </summary>
		/// <param name="mousePT"></param>

		private void MoveRect()
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
					window.SourceInitialized -= AttachWindowToDesktop;
					//change owner to the startup window, which is created anyway
				}
				else
				{
					window.SourceInitialized += AttachWindowToDesktop;
				}
			}
		}
		//?? is it a must to call right after the SourceInitialized phase ?? why relying on the order of "when style is set"
		private static void AttachWindowToDesktop(object sender, EventArgs args)
		{
			if(sender is ScreensShotWindows window)
			{
				new WindowInteropHelper(window)
				{
					Owner = InteropMethods.GetDesktopWindow_()
				};
				
			}
		}
		private static void AttatchWindowToStartup(object sender, EventArgs args)
		{
			if(sender is ScreensShotWindows window)
			{
				new WindowInteropHelper(window)
				{
					Owner = InteropMethods.GetDesktopWindow_()
				};
			}
		}

		public Color GetDesktopImagePixelColor(InteropStructs.POINT pt)
		{
			if(_desktopSnapShot != null)
			{
				return _desktopSnapShot.GetPixelColor(pt.X, pt.Y);
			}
			else return Colors.Transparent;
		}
		#endregion
	}

	public static class BitMapImageLocalSaveExt
	{
		public static void SaveToLocal(this BitmapSource image, string imageName = "", string AbsoluteDirectory = "", string extension = ".bmp")
		{
			try
			{
				if(string.IsNullOrWhiteSpace(AbsoluteDirectory) || !Directory.Exists(AbsoluteDirectory))
				{
					AbsoluteDirectory = AppDomain.CurrentDomain.BaseDirectory;
				}
				if(string.IsNullOrWhiteSpace(imageName))
				{
					imageName = System.IO.Path.GetRandomFileName();
				}
				imageName = System.IO.Path.Combine(AbsoluteDirectory, imageName)+extension;

				if(extension == ".bmp") {
					BitmapEncoder encoder = new BmpBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(image));
					using(FileStream fs = new System.IO.FileStream(imageName, System.IO.FileMode.Create))
					{
						encoder.Save(fs);
						LogSystemShared.LogWriter.WriteLine($"Image has been saved: {imageName}");
					}
				}else if(extension == ".png")
				{
					PngBitmapEncoder encoder = new PngBitmapEncoder();
					encoder.Frames.Add(BitmapFrame.Create(image));
					using(FileStream fs = new System.IO.FileStream(imageName, System.IO.FileMode.Create))
					{
						encoder.Save(fs);
						LogSystemShared.LogWriter.WriteLine($"Image has been saved: {imageName}");
					}
				}
				
			}
			catch(Exception e)
			{
				LogSystemShared.LogWriter.WriteLine($"Exception occured when saving image: {e.Message}", title: "Save Error");
			}
		}
	}
}
