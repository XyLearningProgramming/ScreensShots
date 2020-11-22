using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
					if(param is MoveCommandParameter mc && mc.targetElement == Root.TargetArea) { return Root.WindowStatus == ScreenShotWindowStatus.Empty; }
					else if(param is MoveMagnifierCommandParameter mmc) { return Root.WindowStatus != ScreenShotWindowStatus.IsSelecting; }
					else return true;
				}
			)); }

		private DelegateCommand<ICommandParameter> _updateMouseCursorCommand;
		public DelegateCommand<ICommandParameter> UpdateMouseCursorCommand {get => _updateMouseCursorCommand ?? (_updateMouseCursorCommand = new DelegateCommand<ICommandParameter>(
				(ICommandParameter param) =>
				{
					if(param is UpdateMouseCurosrCommandParameter um)
					{
						UpdateCursorIcon(um.MouoseGlobalPoint, um.SnapLength);
					}
				},
				(ICommandParameter param) =>
				{
					// only available when not holding mouse and drawing
					return Root.WindowStatus != ScreenShotWindowStatus.IsDrawing;
				}
			)); }

		private DelegateCommand<ICommandParameter> _resizeTargetAreaCommand;
		public DelegateCommand<ICommandParameter> ResizeTargetAreaCommand { get => _resizeTargetAreaCommand ?? (_resizeTargetAreaCommand = new DelegateCommand<ICommandParameter>(
				 (ICommandParameter param) =>
				 {
					 if(param is ReSizeTargetAreaCommandParameter rem)
						 MoveTargetAreaTo(rem.TargetRect);
				 },
				 (ICommandParameter param) => 
				 {
					 if(param is ReSizeTargetAreaCommandParameter rem)
						 return Root.WindowStatus == ScreenShotWindowStatus.IsDrawing;
					 return false;
				 }
			)); }
		#region private methods
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
				if(InteropMethods.PtInRect_(ref rect, mousePT) && InteropMethods.IsWindowEnabled_(pair.Item1))
				{
					_mouseOverWindowHandler = pair.Item1;
					mouseOverRect = rect;
					break;
				}
			}
			if(_mouseOverWindowHandler == IntPtr.Zero)
			{ 
				_mouseOverWindowHandler = Root.DesktopWindowHandler; 
				InteropMethods.GetWindowRect_(_mouseOverWindowHandler, out mouseOverRect);
			}

			LogSystemShared.LogWriter.WriteLine(InteropMethods.GetWindowText_(_mouseOverWindowHandler), "Mouse over window ");

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

			if(rect.Right > Root.DesktopWindowRect.Width)
			{
				rect.Left -= rect.Right - Root.DesktopWindowRect.Width;
				rect.Right = Root.DesktopWindowRect.Width;
			}

			if(rect.Bottom > Root.DesktopWindowRect.Height)
			{
				rect.Top -= rect.Bottom - Root.DesktopWindowRect.Height;
				rect.Bottom = Root.DesktopWindowRect.Height;
			}
			// set target area stats
			Root.TargetArea.Width = rect.Width;
			Root.TargetArea.Height = rect.Height;
			Root.TargetArea.Margin = new Thickness(rect.Left, rect.Top, 0, 0);
			Root.TargetAreaSize = rect.GetSize(); // update string size top left

			// move stackpanel below
			Root.StackedButtons.Margin = new Thickness(0, Root.TargetArea.Margin.Top + Root.TargetArea.Height + 5, Root.DesktopWindowRect.Width - Root.TargetArea.Margin.Left - Root.TargetArea.Width -5, 0);

			MoveMaskArea();
		}
		private void MoveMaskArea()
		{
			// let four rects cover all the rest parts in grey
			//  _
			// l l 1 = 1	
			// l_l
			//
			Root.MaskAreaLeft.Width = Root.TargetArea.Margin.Left;
			Root.MaskAreaLeft.Height = Root.TargetArea.Margin.Top + Root.TargetArea.Height;

			Root.MaskAreaTop.Margin = new Thickness(Root.TargetArea.Margin.Left, 0, 0, 0);
			Root.MaskAreaTop.Height = Root.TargetArea.Margin.Top;
			Root.MaskAreaTop.Width = Root.DesktopWindowRect.Width - Root.TargetArea.Margin.Left;

			Root.MaskAreaRight.Margin = new Thickness(Root.TargetArea.Margin.Left + Root.TargetArea.Width, Root.TargetArea.Margin.Top, 0, 0);
			Root.MaskAreaRight.Width = Root.DesktopWindowRect.Width - Root.TargetArea.Margin.Left - Root.TargetArea.Width;
			Root.MaskAreaRight.Height = Root.DesktopWindowRect.Height - Root.TargetArea.Margin.Top;

			Root.MaskAreaBottom.Margin = new Thickness(0, Root.TargetArea.Height + Root.TargetArea.Margin.Top, 0, 0);
			Root.MaskAreaBottom.Width = Root.TargetArea.Margin.Left + Root.TargetArea.Width;
			Root.MaskAreaBottom.Height = Root.DesktopWindowRect.Height - Root.MaskAreaBottom.Margin.Top;

//#if DEBUG
//			LogRect(Root.TargetArea);
//			LogRect(Root.MaskAreaLeft); LogRect(Root.MaskAreaTop); LogRect(Root.MaskAreaRight); LogRect(Root.MaskAreaBottom);
//#endif
		}

		private void MoveMagnifier(MoveMagnifierCommandParameter param)
		{
			Root.Magnifier.Margin = new Thickness(param.targetPoint.X + param.Offset.X, param.targetPoint.Y + param.Offset.Y, 0, 0);
			Root.MagnifierPreviewVisualBrush.Viewbox = new Rect(new Point(param.targetPoint.X - param.ViewboxSize.Width / 2 + 0.5, param.targetPoint.Y - param.ViewboxSize.Height / 2 + 0.5), param.ViewboxSize);
			// update it's hint string POS: (1000,1000) RGB: (255,255,255)
			StringBuilder hint = new StringBuilder();
			hint.Append($"Pos: ({param.targetPoint.X},{param.targetPoint.Y})  ");
			Color pixel = Root.GetDesktopImagePixelColor(param.targetPoint);
			hint.Append($"RGBA: ({pixel.R},{pixel.G},{pixel.B},{pixel.A})");
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
			// paste from handycontrol's control
			Cursor cursor;
			InteropStructs.RECT targetAreaRect = Root.TargetAreaRECT;

			var leftAbs = Math.Abs(mousePT.X);
			var topAbs = Math.Abs(mousePT.Y);
			var rightAbs = Math.Abs(mousePT.X - targetAreaRect.Width);
			var downAbs = Math.Abs(mousePT.Y - targetAreaRect.Height);

			//_canDrag = false;
			//_isOut = false;
			//_flagArr[0] = 0;
			//_flagArr[1] = 0;
			//_flagArr[2] = 0;
			//_flagArr[3] = 0;

			if(leftAbs <= SnapLength)
			{
				if(topAbs > SnapLength)
				{
					if(downAbs > SnapLength)
					{
						// left
						cursor = Cursors.SizeWE;
						//_pointFixed = new Point(_targetWindowRect.Right, _targetWindowRect.Top);
						//_pointFloating = new InteropValues.POINT(_targetWindowRect.Left, _targetWindowRect.Bottom);
						//_flagArr[0] = 1;
					}
					else
					{
						//left bottom
						cursor = Cursors.SizeNESW;
						//_pointFixed = new Point(_targetWindowRect.Right, _targetWindowRect.Top);
						//_pointFloating = new InteropValues.POINT(_targetWindowRect.Left, _targetWindowRect.Bottom);
						//_flagArr[0] = 1;
						//_flagArr[3] = 1;
					}
				}
				else
				{
					// left top
					cursor = Cursors.SizeNWSE;
					//_pointFixed = new Point(_targetWindowRect.Right, _targetWindowRect.Bottom);
					//_pointFloating = new InteropValues.POINT(_targetWindowRect.Left, _targetWindowRect.Top);
					//_flagArr[0] = 1;
					//_flagArr[1] = 1;
				}
			}
			else if(rightAbs > SnapLength)
			{
				if(topAbs > SnapLength)
				{
					if(downAbs > SnapLength)
					{
						if(Root.TargetArea.IsMouseOver)
						{
							//drag
							cursor = Cursors.SizeAll;
							//_canDrag = true;
						}
						else
						{
							//out
							cursor = Cursors.Arrow;
							//_isOut = true;
						}
					}
					else
					{
						//bottom
						cursor = Cursors.SizeNS;
						//_pointFixed = new Point(_targetWindowRect.Left, _targetWindowRect.Top);
						//_pointFloating = new InteropValues.POINT(_targetWindowRect.Right, _targetWindowRect.Bottom);
						//_flagArr[3] = 1;
					}
				}
				else
				{
					//top
					cursor = Cursors.SizeNS;
					//_pointFixed = new Point(_targetWindowRect.Right, _targetWindowRect.Bottom);
					//_pointFloating = new InteropValues.POINT(_targetWindowRect.Left, _targetWindowRect.Top);
					//_flagArr[1] = 1;
				}
			}
			else if(rightAbs <= SnapLength)
			{
				if(topAbs > SnapLength)
				{
					if(downAbs > SnapLength)
					{
						//right
						cursor = Cursors.SizeWE;
						//_pointFixed = new Point(_targetWindowRect.Left, _targetWindowRect.Bottom);
						//_pointFloating = new InteropValues.POINT(_targetWindowRect.Right, _targetWindowRect.Top);
						//_flagArr[2] = 1;
					}
					else
					{
						//right bottom
						cursor = Cursors.SizeNWSE;
						//_pointFixed = new Point(_targetWindowRect.Left, _targetWindowRect.Top);
						//_pointFloating = new InteropValues.POINT(_targetWindowRect.Right, _targetWindowRect.Bottom);
						//_flagArr[2] = 1;
						//_flagArr[3] = 1;
					}
				}
				else
				{
					// right top
					cursor = Cursors.SizeNESW;
					//_pointFixed = new Point(_targetWindowRect.Left, _targetWindowRect.Bottom);
					//_pointFloating = new InteropValues.POINT(_targetWindowRect.Right, _targetWindowRect.Top);
					//_flagArr[1] = 1;
					//_flagArr[2] = 1;
				}
			}
			else
			{
				//out
				cursor = Cursors.Arrow;
				//_isOut = true;
			}

			Root.Cursor = cursor;
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

	internal class UpdateMouseCurosrCommandParameter : ICommandParameter { public InteropStructs.POINT MouoseGlobalPoint; public int SnapLength = 4; }
}
