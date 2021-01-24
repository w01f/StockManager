using System;
using System.Reflection;
using System.Windows.Input;

namespace StockManager.TradingAdvisor.Models
{
    class CommonCommand: ICommand
    {
		private readonly Action _commandAction;
		
		public event EventHandler CanExecuteChanged;

		public CommonCommand(Action commandAction)
		{
			_commandAction = commandAction;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			try
			{
				_commandAction?.DynamicInvoke();
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException ?? ex;
			}
		}
	}
}
