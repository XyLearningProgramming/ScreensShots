using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;

namespace ScreenShotWindows
{
	internal class ScreenShotWindowsCommands
	{
		// handler of window mouse is positioned (excluding screenshot windows itself)
		IntPtr _mouseOverWindowHandler = IntPtr.Zero;

		internal ScreensShotWindows Root { get; private set; }
		public ScreenShotWindowsCommands(ScreensShotWindows screensShotWindows)
		{
			Root = screensShotWindows;
		}

		#region stackedButtons
		private DelegateCommand<TryChangeTargetAreaCommandParameter> _tryChangeTargetAreaSize;
		public DelegateCommand<TryChangeTargetAreaCommandParameter> TryChangeTargetAreaSize { get =>
			 _tryChangeTargetAreaSize ?? (_tryChangeTargetAreaSize = new DelegateCommand<TryChangeTargetAreaCommandParameter>(
					(param) => {
					// 1. rescale this back to original size if needed
					// 2. clip 
					// 3. change binding objects through clip command
						InteropStructs.RECT rect = Root.TargetAreaRECT;
						var rescaledWH = Root.GetPointBeforeScaling(new InteropStructs.POINT(param.intendedWidth, param.intendedHeight));
						rect.Width = rescaledWH.X;
						rect.Height = rescaledWH.Y;
						MoveTargetAreaTo(rect);
					},
					(_) => { return Root.WindowStatus == ScreenShotWindowStatus.IsSelecting; }
					));
		}
		private DelegateCommand<EmptyCommandParameter> _targetAreaYesClickedCommand;
		public DelegateCommand<EmptyCommandParameter> TargetAreaYesClickedCommand {get => _targetAreaYesClickedCommand ?? (_targetAreaYesClickedCommand = new DelegateCommand<EmptyCommandParameter>(
			(_) => { Root.SaveScreenShots = true; Root.Close(); }
			)); }
		private DelegateCommand<EmptyCommandParameter> _targetAreaNoClickedCommand;
		public DelegateCommand<EmptyCommandParameter> TargetAreaNoClickedCommand
		{
			get => _targetAreaNoClickedCommand ?? (_targetAreaNoClickedCommand = new DelegateCommand<EmptyCommandParameter>(
			(_) => { Root.SaveScreenShots = false; Root.Close(); }
			));
		}
		private DelegateCommand<EmptyCommandParameter> _saveAsClickedCommand;
		public DelegateCommand<EmptyCommandParameter> SaveAsClickedCommand {get => _saveAsClickedCommand ?? (_saveAsClickedCommand = new DelegateCommand<EmptyCommandParameter>(
			(_) => 
			{
				var SaveFileDialog = new System.Windows.Forms.SaveFileDialog()
				{
					InitialDirectory = Root.UserSettings.ImageFolderPath,
					Filter = GetFilterString(Root.UserSettings.SaveFormatPreferred),
					FileName = System.IO.Path.GetRandomFileName(),
					DefaultExt = "." + Root.UserSettings.SaveFormatPreferred,
				};
				if(SaveFileDialog.ShowDialog()== System.Windows.Forms.DialogResult.OK)
				{
					InteropStructs.RECT rect = Root.TargetAreaRECT;
					rect = Root.GetRectAfterScaling(rect);
					try
					{
						CroppedBitmap image = new CroppedBitmap(Root.SnappedImage, new Int32Rect(rect.Left, rect.Top, rect.Width, rect.Height));
						var fileInfo = new FileInfo(SaveFileDialog.FileName);
						image.SaveToLocal(AbsoluteDirectory: fileInfo.DirectoryName, imageName: Path.GetFileNameWithoutExtension(fileInfo.Name), extension: fileInfo.Extension);
						Clipboard.SetImage(image);
					}
					catch(Exception e)
					{
						LogSystemShared.LogWriter.WriteLine(e.Message, "Crop error");
						if(Root.SnappedImage != null)
						{
							var fileInfo = new FileInfo(SaveFileDialog.FileName);
							Root.SnappedImage.SaveToLocal(AbsoluteDirectory: fileInfo.DirectoryName, imageName: Path.GetFileNameWithoutExtension(fileInfo.Name), extension: fileInfo.Extension);
							Clipboard.SetImage(Root.SnappedImage);
						}
					}
				}
			}
			)); }
		#endregion

		//private DelegateCommand<InteropStructs.POINT, object> _moveTargetArea;
		//public DelegateCommand<InteropStructs.POINT, object> MoveTargetAreaCommand {get => _moveTargetArea ?? (_moveTargetArea =
		//		new DelegateCommand<InteropStructs.POINT, object>(
		//			pt => MoveTargetAreaExec(pt), (_) => IsTargetAreaMoving
		//			));
		//}
		private DelegateCommand<ICommandParameter> _moveCommand;
		public DelegateCommand<ICommandParameter> MoveCommand {get => _moveCommand ?? (_moveCommand = new DelegateCommand<ICommandParameter>(
				(ICommandParameter param) => 
				{
					if(param is MoveCommandParameter mc && mc.targetElement == Root.TargetArea)
					{
						SnapTargetAreaExec(mc.targetPoint);
					}
					else if(param is MoveMagnifierCommandParameter mmc && mmc.targetElement == Root.Magnifier)
					{
						MoveMagnifier(mmc);
					}
				},
				(ICommandParameter param) => 
				{
					if(param is MoveCommandParameter mc && mc.targetElement == Root.TargetArea && IsPointInFrameworkElement(mc.targetPoint, Root)) { return Root.WindowStatus == ScreenShotWindowStatus.Empty; }
					else if(param is MoveMagnifierCommandParameter mmc ) 
					{
						if(IsPointInFrameworkElement(mmc.targetPoint, Root) && Root.WindowStatus != ScreenShotWindowStatus.IsSelecting)
						{
							return true;
						}
						else
						{
							return false;
						}
					}
					else return false;
				}
			)); }

		private DelegateCommand<ICommandParameter> _updateMouseCursorCommand;
		public DelegateCommand<ICommandParameter> UpdateMouseCursorCommand {get => _updateMouseCursorCommand ?? (_updateMouseCursorCommand = new DelegateCommand<ICommandParameter>(
				(ICommandParameter param) =>
				{
					if(param is UpdateMouseCurosrCommandParameter um)
					{
						UpdateCursorIcon(um.MouseLocalPoint, um.SnapLength);
					}
				},
				(ICommandParameter param) =>
				{
					// only available when not holding mouse and drawing
					return (Root.WindowStatus != ScreenShotWindowStatus.IsDrawing) && IsPointInFrameworkElement(new InteropStructs.POINT(Mouse.GetPosition(Root)), Root);
				}
			)); }

		private DelegateCommand<ICommandParameter> _resizeTargetAreaCommand;
		public DelegateCommand<ICommandParameter> ResizeTargetAreaCommand { get => _resizeTargetAreaCommand ?? (_resizeTargetAreaCommand = new DelegateCommand<ICommandParameter>(
				 (ICommandParameter param) =>
				 {
					 if(param is ReSizeTargetAreaCommandParameter rem)
					 {
						 MoveTargetAreaTo(rem.TargetRect);
					 }
				 },
				 (ICommandParameter param) => 
				 {
					 if(param is ReSizeTargetAreaCommandParameter rem)
						 return (Root.WindowStatus != ScreenShotWindowStatus.Empty) && IsPointInFrameworkElement(new InteropStructs.POINT(Mouse.GetPosition(Root)), Root);
					 return false;
				 }
			)); }
		#region private methods

		#region stacked button control
		private Dictionary<string, string> _formatToFilterDict = new Dictionary<string, string>() { ["png"] = "PNG (*.png)", ["jpeg"] = "JPEG (*jpeg)", ["bmp"] = "BMP (*.bmp)" };
		private	string GetFilterString(string preferredFormat)
		{
			Debug.Assert(_formatToFilterDict.ContainsKey(preferredFormat));
			List<string> filterList = new List<string>() { _formatToFilterDict[preferredFormat] };
			foreach(var fl in _formatToFilterDict)
			{
				if(!filterList.Contains(fl.Value)) filterList.Add(fl.Value);
			}
			return string.Join('|', filterList);
		}
		#endregion
		private void SnapTargetAreaExec(InteropStructs.POINT mousePT)
		{
			//// !!! This proves to always return our screenshot winow on top
			//IntPtr mouseOverWindowHandler = InteropMethods.ChildWindowFromPointEx_(Root.DesktopWindowHandler, mousePT, InteropStructs.CHILDWINDOWEXTFlAG.CWP_SKIPDISABLED | InteropStructs.CHILDWINDOWEXTFlAG.CWP_SKIPINVISIBLE);
//			if(mouseOverWindowHandler!=IntPtr.Zero && mouseOverWindowHandler != _mouseOverWindowHandler)
//			{
//#if DEBUG
//				LogSystemShared.LogWriter.WriteLine(InteropMethods.GetWindowText_(mouseOverWindowHandler),"Mouse over window ");
//				if(mouseOverWindowHandler == new WindowInteropHelper(Root).Handle)
//				{
//					LogSystemShared.LogWriter.WriteLine("Mouse over screenshot window itself");
//				}
//				if(mouseOverWindowHandler == new WindowInteropHelper(Root).Handle)
//				{
//					LogSystemShared.LogWriter.WriteLine("Mouse over screenshot window itself");
//				}
//#endif
				// check if point in any window

			_mouseOverWindowHandler = IntPtr.Zero;
			InteropStructs.RECT mouseOverRect = new InteropStructs.RECT(0,0,0,0);
			foreach(var pair in Root.AllWinHandlers)
			{
				InteropStructs.RECT rect = pair.Item2;
				if(InteropMethods.PtInRect_(ref rect, mousePT))
				{
					_mouseOverWindowHandler = pair.Item1;
					mouseOverRect = new InteropStructs.RECT(rect);
					break;
				}
			}
			if(_mouseOverWindowHandler == IntPtr.Zero)
			{
				mouseOverRect = Root.ThisRECT;
			}

			//LogSystemShared.LogWriter.WriteLine(InteropMethods.GetWindowText_(_mouseOverWindowHandler), "Mouse over window ");

			MoveTargetAreaTo(mouseOverRect);
		}
		private void MoveTargetAreaTo(InteropStructs.RECT rect)
		{
			// clip rect according to desktop bounds
			if(rect.Left < 0)
			{
				rect.Right -= rect.Left;
				rect.Left = 0;
			}

			if(rect.Top < 0)
			{
				rect.Bottom -= rect.Top;
				rect.Top = 0;
			}

			if(rect.Right > (int)Root.Width)
			{
				rect.Left -= rect.Right - (int)Root.Width;
				rect.Right = (int)Root.Width;
			}

			if(rect.Bottom > (int)Root.Height)
			{
				rect.Top -= rect.Bottom - (int)Root.Height;
				rect.Bottom = (int)Root.Height;
			}
			// set target area stats
			Root.TargetArea.Width = rect.Width;
			Root.TargetArea.Height = rect.Height;
			Root.TargetArea.Margin = new Thickness(rect.Left, rect.Top, 0, 0);
			Root.TargetAreaSize = Root.GetRectAfterScaling(rect).GetSize(); // update string size top left

			// move stackpanel below
			//Root.StackedButtons.Margin = new Thickness(0, Root.TargetArea.Margin.Top + Root.TargetArea.Height + 5, Root.DesktopWindowRect.Width - Root.TargetArea.Margin.Left - Root.TargetArea.Width -5, 0);

			MoveStackButtons(Root.TargetArea.Margin.Top, Root.Width- Root.TargetArea.Margin.Left- Root.TargetArea.Width, new InteropStructs.POINT(-5,5));
			// change stackbuttons binding
			var scaledPT = Root.GetPointAfterScaling(new InteropStructs.POINT(rect.Width, rect.Height));
			Root.InputHeightValue = scaledPT.Y;
			Root.InputWidthValue = scaledPT.X;

			MoveMaskArea();
		}
		private void MoveMaskArea()
		{
			// let four rects cover all the rest parts in grey
			//  _
			// l l 1 = 1	
			// l_l
			//
			Root.MaskAreaLeft.Width = Math.Max(Root.TargetArea.Margin.Left,0);
			Root.MaskAreaLeft.Height = Math.Max(0,Root.TargetArea.Margin.Top + Root.TargetArea.Height);

			Root.MaskAreaTop.Margin = new Thickness(Math.Max(Root.TargetArea.Margin.Left,0), 0, 0, 0);
			Root.MaskAreaTop.Height = Math.Max(Root.TargetArea.Margin.Top,0);
			Root.MaskAreaTop.Width = Math.Max(Root.Width - Root.TargetArea.Margin.Left,0);

			Root.MaskAreaRight.Margin = new Thickness(Math.Max(Root.TargetArea.Margin.Left + Root.TargetArea.Width,0), Math.Max(Root.TargetArea.Margin.Top,0), 0, 0);
			Root.MaskAreaRight.Width = Math.Max(Root.Width - Root.TargetArea.Margin.Left - Root.TargetArea.Width,0);
			Root.MaskAreaRight.Height = Math.Max(Root.Height - Root.TargetArea.Margin.Top,0);

			Root.MaskAreaBottom.Margin = new Thickness(0, Math.Max(Root.TargetArea.Height + Root.TargetArea.Margin.Top,0), 0, 0);
			Root.MaskAreaBottom.Width = Math.Max(Root.TargetArea.Margin.Left + Root.TargetArea.Width,0);
			Root.MaskAreaBottom.Height = Math.Max(Root.Height - Root.MaskAreaBottom.Margin.Top, 0);

//#if DEBUG
//			LogRect(Root.TargetArea);
//			LogRect(Root.MaskAreaLeft); LogRect(Root.MaskAreaTop); LogRect(Root.MaskAreaRight); LogRect(Root.MaskAreaBottom);
//#endif
		}

		private void MoveStackButtons(double Top, double Right, InteropStructs.POINT offset)
		{
			if(Root.StackedButtons.Visibility != Visibility.Visible) return; // not visible not moving

			bool isFlippingUp = Root.StackedButtons.Height + Top + offset.Y >= Root.Height ? true : false;
			bool isFlippingRight = Root.StackedButtons.Width + Right + offset.X >= Root.Width? true: false;
			if(!isFlippingRight && !isFlippingUp)
				Root.StackedButtons.Margin = new Thickness(0, Top + offset.Y, Right + offset.X, 0);
			else if(isFlippingRight && !isFlippingUp)
			{
				Root.StackedButtons.Margin = new Thickness(0, Top + offset.Y, Right - offset.X - Root.StackedButtons.Width, 0);
			}
			else if(!isFlippingRight && isFlippingUp)
			{
				Root.StackedButtons.Margin = new Thickness(0, Top - offset.Y - Root.StackedButtons.Height, Right + offset.X, 0);
			}
			else
			{
				Root.StackedButtons.Margin = new Thickness(0, Top - offset.Y - Root.StackedButtons.Height, Right - offset.X - Root.StackedButtons.Width, 0);
			}
		}

		private void MoveMagnifier(MoveMagnifierCommandParameter param)
		{
			Root.MagnifierPreviewVisualBrush.Viewbox = new Rect(new Point(param.targetPoint.X - param.ViewboxSize.Width / 2 + 0.5, param.targetPoint.Y - param.ViewboxSize.Height / 2 + 0.5), param.ViewboxSize);

			bool flipUp = false, flipLeft = false;
			if(param.targetPoint.Y + param.Offset.Y + Root.Magnifier.ActualHeight >= Root.ActualHeight) flipUp = true;
			if(param.targetPoint.X + param.Offset.X + Root.Magnifier.ActualWidth >= Root.ActualWidth) flipLeft = true;
			if(flipUp && flipLeft)
			{
				Root.Magnifier.Margin = new Thickness(param.targetPoint.X - param.Offset.X - Root.Magnifier.ActualWidth, param.targetPoint.Y - param.Offset.Y - Root.Magnifier.ActualHeight, 0, 0);
			}
			else if(flipUp)
			{
				Root.Magnifier.Margin = new Thickness(param.targetPoint.X + param.Offset.X, param.targetPoint.Y - param.Offset.Y - Root.Magnifier.ActualHeight, 0, 0);
			}
			else if(flipLeft)
			{
				Root.Magnifier.Margin = new Thickness(param.targetPoint.X - param.Offset.X - Root.Magnifier.ActualWidth, param.targetPoint.Y + param.Offset.Y, 0, 0);
			}
			else
				Root.Magnifier.Margin = new Thickness(param.targetPoint.X + param.Offset.X, param.targetPoint.Y + param.Offset.Y, 0, 0);

			// update it's hint string POS: (1000,1000) RGB: (255,255,255)
			StringBuilder hint = new StringBuilder();
			InteropStructs.POINT scaled = Root.GetPointAfterScaling(param.targetPoint);
			hint.Append($"Pos: ({scaled.X},{scaled.Y})    ");
			Color pixel = Root.GetDesktopImagePixelColor(param.targetPoint);
			hint.Append($"RGB: ({pixel.R},{pixel.G},{pixel.B})");
			//hint.Append(Mouse.DirectlyOver.GetType());
			Root.MagnifierHintText = hint.ToString();
		}

		#if DEBUG
		private void LogRect(FrameworkElement elem)
		{
			LogSystemShared.LogWriter.WriteLine($"Rect at Point {elem.Margin.Top}*{elem.Margin.Left}, width {elem.Width} and {elem.Height}");
		}
		#endif
		private void UpdateCursorIcon(InteropStructs.POINT mousePT, int SnapLength)
		{
			Cursor cursor;
			InteropStructs.RECT targetAreaRect = Root.TargetAreaRECT;

			bool leftAbs = Math.Abs(mousePT.X - targetAreaRect.Left) <= SnapLength;
			bool topAbs = Math.Abs(mousePT.Y - targetAreaRect.Top) <= SnapLength;
			bool rightAbs = Math.Abs(mousePT.X - targetAreaRect.Right) <= SnapLength;
			bool downAbs = Math.Abs(mousePT.Y - targetAreaRect.Bottom) <= SnapLength;
			if(!leftAbs && !topAbs && !rightAbs && !downAbs)
			{
				if(InteropMethods.PtInRect_(ref targetAreaRect, mousePT))
				{
					cursor = Cursors.SizeAll;
				}
				else
				{
					cursor = Cursors.Arrow;
				}
			}
			else if((leftAbs && topAbs) || (rightAbs && downAbs))
			{
				cursor = Cursors.SizeNWSE;
			}
			else if((topAbs && rightAbs) || (downAbs && leftAbs))
			{
				cursor = Cursors.SizeNESW;
			}
			else if(topAbs || downAbs)
			{
				cursor = Cursors.SizeNS;
			}
			else if(rightAbs || leftAbs)
			{
				cursor = Cursors.SizeWE;
			}
			else cursor = Cursors.Arrow;

			Root.Cursor = cursor;
		}

		private static bool IsPointInFrameworkElement(InteropStructs.POINT relativePoint, FrameworkElement element)
		{
			InteropStructs.RECT rect = new InteropStructs.RECT((int)element.Margin.Left, (int)element.Margin.Top, (int)element.Margin.Left + (int)element.ActualWidth, (int)element.Margin.Top + (int)element.ActualHeight);
			return InteropMethods.PtInRect_(ref rect, relativePoint);
		}
		#endregion
	}

	/// <summary>
	/// DelegateCommand simply implements ICommand
	/// </summary>
	internal class DelegateCommand<T>: ICommand where T : ICommandParameter
	{
		private readonly Action<T> _execute;
		private readonly Func<T, bool> _canExecute;
		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action<T> execute) : this(execute, null)
		{
		}
		public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
		{
			if(execute == null) throw new ArgumentNullException(nameof(execute));
			_execute = execute;
			_canExecute = canExecute;
		}
		public bool CanExecute(object parameter)
		{
			return _canExecute == null ? true : _canExecute((T)parameter);
		}

		public bool CanExecute(T parameter)
		{
			return _canExecute == null ? true : _canExecute(parameter);
		}

		public void Execute(T parameter)
		{
			_execute(parameter);
		}
		public void RaiseCanExecuteChanged()
		{
			this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
		public void Execute(object parameter)
		{
			Execute((T)parameter);
		}
	}

	internal static class DelegateCommandTryExecExt 
	{ 
		public static void TryExecute(this DelegateCommand<ICommandParameter> command, ICommandParameter param)
		{
			if(command.CanExecute(param))
			{
				command.Execute(param);
			}
		}
	}

	internal interface ICommandParameter { }
	internal class MoveCommandParameter : ICommandParameter 
	{
		public FrameworkElement targetElement;
		public InteropStructs.POINT targetPoint;
	}
	internal class MoveMagnifierCommandParameter : MoveCommandParameter 
	{
		public InteropStructs.POINT Offset;
		public Size ViewboxSize;
	}
	internal class ReSizeTargetAreaCommandParameter: ICommandParameter
	{
		public InteropStructs.RECT TargetRect;
	}

	internal class UpdateMouseCurosrCommandParameter : ICommandParameter { public InteropStructs.POINT MouseLocalPoint; public int SnapLength = 4; }

	internal class TryChangeTargetAreaCommandParameter : ICommandParameter { public int intendedWidth; public int intendedHeight; }

	internal class EmptyCommandParameter : ICommandParameter { }
}
