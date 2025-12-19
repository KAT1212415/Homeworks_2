using Autodesk.Revit.DB;
using ClassLibrary9.Abstractions;
using ClassLibrary9.Models;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Windows.Input;


namespace ClassLibrary9.ViewModels
{
    /// <summary>
    /// Модель представления главного окна
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {

        private readonly ISelectionServices _selectionService;
        private readonly IGeometryService _geometryService;

        /// <summary>
        /// Инициализирует новый экземпляр класса MainWindowViewModel
        /// </summary>
        /// <param name="selectionService">Сервис выбора элементов</param>
        /// <param name="geometryService">Сервис работы с геометрией</param>
        public MainWindowViewModel(ISelectionServices selectionService, IGeometryService geometryService)
        {

            CalcOpening = new RelayCommand(OnCalcOpeningExecute);
            _selectionService = selectionService;
            _geometryService = geometryService;
        }

        /// <summary>
        /// Событие, возникающее при изменении значения свойства
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private OpeningInfo _openingInfo;
        /// <summary>
        /// Информация о выбранном проеме
        /// </summary>
        public OpeningInfo OpeningInfo
        {
            get => _openingInfo;
            set
            {
                _openingInfo = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage = "Выберите проем";
        /// <summary>
        /// Сообщение о текущем статусе операции
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        private double _limit = 1000;

        /// <summary>
        /// Ограничение по расстоянию от верха проема до верха стены в миллиметрах
        /// </summary>
        public double Limit
        {
            get => _limit;
            set
            {
                _limit = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Команда для расчета информации о проеме
        /// </summary>
        public ICommand CalcOpening { get; }
        private void OnCalcOpeningExecute(object parameter)
        {
            Wall opening = _selectionService.PickOpening();
            if (opening == null)
            {
                return;
            }

            OpeningInfo = _geometryService.GetOpeningInfo(opening, Limit);

            StatusMessage = $"Обработан {OpeningInfo.WallName}";
        }
    }
}