
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClassLibrary15.Services;
using ClassLibrary15.ViewModels;
using ClassLibrary15.Views;

namespace ClassLibrary15
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Cmd : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            // 1. Получаем документ Revit из commandData
            Document doc = commandData.Application.ActiveUIDocument.Document;

            // 2. Создаём сервис и передаём ему документ
            var placementService = new PlacementService(doc);

            // 3. Создаём ViewModel
            var viewModel = new MainWindowViewModel(placementService);

            // 4. Создаём окно
            var window = new MainWindow(viewModel);

            // 5. Показываем окно
            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}