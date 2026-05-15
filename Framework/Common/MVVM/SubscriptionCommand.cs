using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Aitex.Core.UI.MVVM
{
    public class SubscriptionCommand<T> : ICommand
    {
        readonly Action<T> _execute;
        readonly Predicate<T> _canExecute;
        public event EventHandler CanExecuteChanged;

        public SubscriptionCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        public SubscriptionCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }


        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute((T)parameter);
        }
        

        // The CanExecuteChanged is automatically registered by command binding, we can assume that it has some execution logic 
        // to update the button's enabled\disabled state(though we cannot see). So raises this event will cause the button's state be updated.
        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

    }
}