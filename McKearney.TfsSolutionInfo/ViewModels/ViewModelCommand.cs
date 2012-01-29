using System;
using System.Windows.Input;

namespace McKearney.TfsSolutionInfo.ViewModels
{
    public class ViewModelCommand : ICommand
    {
        public ViewModelCommand(Action<object> executeAction,
                                Predicate<object> canExecute = null)
        {
            if (executeAction == null)
                throw new ArgumentNullException("executeAction");
            _executeAction = executeAction;
            _canExecute = canExecute;
        }
        private readonly Predicate<object> _canExecute;
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;
            return _canExecute(parameter);
        }
        public event EventHandler CanExecuteChanged;
        public void OnCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }
        private readonly Action<object> _executeAction;
        public void Execute(object parameter)
        {
            _executeAction(parameter);
        }
    }
}
