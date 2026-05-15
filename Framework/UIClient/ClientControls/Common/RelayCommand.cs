using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MECF.Framework.UI.Client.ClientControls.Common
{
    public class RelayCommand : ICommand
    {
        private readonly Func<Object, Boolean> canExecute;
        private readonly Action<Object> execute;

        public RelayCommand(Action<Object> execute) : this(execute, null)
        {
        }

        public RelayCommand(Action<Object> execute, Func<Object, Boolean> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException("execute");
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (canExecute != null)
                    CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (canExecute != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        public Boolean CanExecute(Object parameter)
        {
            return canExecute == null ? true : canExecute(parameter);
        }

        public void Execute(Object parameter)
        {
            execute(parameter);
        }
    }
}
