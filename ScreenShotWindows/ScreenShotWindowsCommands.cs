using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScreenShotWindows
{
	internal class ScreenShotWindowsCommands
	{
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
						MoveTargetAreaExec(mc.targetPoint);
					}
					else if(param is MoveMagnifierCommandParameter mmc && mmc.targetElement == Root.Magnifier)
					{
						MoveMagnifier(mmc);
					}
				},
				(ICommandParameter param) => 
				{
					if(param is MoveCommandParameter mc && mc.targetElement == Root.TargetArea) { return !Root.IsSelecting && !Root.IsDrawing; }
					else return true;
				}
			)); }


		#region private methods
		private void MoveTargetAreaExec(InteropStructs.POINT mousePT)
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
			foreach(var pair in Root.AllWinHandlers)
			{
				InteropStructs.RECT rect = pair.Value;
				if(InteropMethods.PtInRect_(ref rect, mousePT))
				{
					_mouseOverWindowHandler = pair.Key;
					break;
				}
			}
			if(_mouseOverWindowHandler == IntPtr.Zero) _mouseOverWindowHandler = Root.DesktopWindowHandler;

			LogSystemShared.LogWriter.WriteLine(InteropMethods.GetWindowText_(_mouseOverWindowHandler), "Mouse over window ");

			InteropMethods.GetWindowRect_(_mouseOverWindowHandler, out InteropStructs.RECT mouseOverRect);
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
			Root.TargetArea.Height = rect.Height+50;
			Root.TargetArea.Margin = new Thickness(rect.Left, rect.Top, 0, 0);
			Root.Size = rect.GetSize(); // update string size top left

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

#if DEBUG
			LogRect(Root.TargetArea);
			LogRect(Root.MaskAreaLeft); LogRect(Root.MaskAreaTop); LogRect(Root.MaskAreaRight); LogRect(Root.MaskAreaBottom);
#endif
		}

		private void MoveMagnifier(MoveMagnifierCommandParameter param)
		{
			Root.Magnifier.Margin = new Thickness(param.targetPoint.X + param.Offset.X, param.targetPoint.Y + param.Offset.Y, 0, 0);
			Root.MagnifierPreviewVisualBrush.Viewbox = new Rect(new Point(param.targetPoint.X - param.ViewboxSize.Width / 2 + 0.5, param.targetPoint.Y - param.ViewboxSize.Height / 2 + 0.5), param.ViewboxSize);
		}

		#if DEBUG
		private void LogRect(FrameworkElement elem)
		{
			LogSystemShared.LogWriter.WriteLine($"Rect at Point {elem.Margin.Top}*{elem.Margin.Left}, width {elem.Width} and {elem.Height}");
		}
		#endif
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
}
