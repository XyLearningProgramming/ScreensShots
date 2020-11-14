using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace ScreenShotApp.MVVMUtils
{
	/// <summary>
	/// DelegateCommand simply implements ICommand, no magic. You can also manually call RaiseCanExecuteChanged() when the command's can execute status changed
	/// </summary>
	public class DelegateCommand: ICommand
	{
		private readonly Action<object> _execute;
		private readonly Func<object, bool> _canExecute;
		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action<object> execute) : this(execute, null)
		{
		}
		public DelegateCommand(Action<object> execute, Func<object,bool> canExecute)
		{
			if(execute == null) throw new ArgumentNullException(nameof(execute));
			_execute = execute;
			_canExecute = canExecute;
		}
		public bool CanExecute(object parameter)
		{
			return _canExecute == null ? true : _canExecute(parameter);
		}

		public void Execute(object parameter)
		{
			_execute(parameter);
		}
		public void RaiseCanExecuteChanged()
		{
			this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
