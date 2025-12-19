using ClassLibrary9.ViewModels;
using System.Windows;


namespace ClassLibrary9.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow: Window
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса MainWindow
        /// </summary>
        /// <param name="mainWindowViewModel">Модель представления главного окна</param>
        public MainWindow(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();
            this.DataContext = mainWindowViewModel;
            
        }
    }
}
