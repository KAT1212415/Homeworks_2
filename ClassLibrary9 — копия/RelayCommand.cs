using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClassLibrary9
{
    /// <summary>
    /// Реализация команды для привязки к элементам интерфейса
    /// </summary>
    internal class RelayCommand : ICommand
    {
        private readonly Action<object> _exectute;
        private readonly Func<object, bool> _canExecute;

        /// <summary>
        /// Событие, возникающее при изменении возможности выполнения команды
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса RelayCommand
        /// </summary>
        /// <param name="exectute">Действие для выполнения</param>
        /// <param name="canExecute">Функция проверки возможности выполнения команды</param>
        public RelayCommand(Action<object> exectute, Func<object, bool> canExecute = null)
        {
            _exectute = exectute ?? throw new ArgumentNullException(nameof(exectute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Определяет, может ли команда выполняться
        /// </summary>
        /// <param name="parameter">Параметр команды</param>
        /// <returns>true, если команда может быть выполнена; иначе false</returns>
        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        /// <summary>
        /// Выполняет команду
        /// </summary>
        /// <param name="parameter">Параметр команды</param>
        public void Execute(object parameter) => _exectute(parameter);
    }
}
