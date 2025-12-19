using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClassLibrary9.Abstractions;
using ClassLibrary9.Services;
using ClassLibrary9.ViewModels;
using ClassLibrary9.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ClassLibrary9
{

    /// <summary>
    /// Команда Revit для работы с проемами в стенах
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    internal class Cmd : IExternalCommand
    {
        /// <summary>
        /// Выполняет команду, инициализирует сервисы и открывает главное окно
        /// </summary>
        /// <param name="commandData">Данные команды Revit</param>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="elements">Набор элементов</param>
        /// <returns>Результат выполнения команды</returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //ServiceCollection services = new ServiceCollection();
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddSingleton<ExternalCommandData>(commandData);
            services.AddSingleton<ISelectionServices, SelectionService>();
            services.AddSingleton<IGeometryService, GeometryService>();
            services.AddSingleton<MainWindowViewModel, MainWindowViewModel>();
            services.AddSingleton<MainWindow, MainWindow>();
            var provider = services.BuildServiceProvider();

            var mainWindow = provider.GetRequiredService<MainWindow>();

            mainWindow.Show();
            return Result.Succeeded;
        }
    }
}
