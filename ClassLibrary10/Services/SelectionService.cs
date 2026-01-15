using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using ClassLibrary10.Abstractions;

namespace ClassLibrary10.Services
{
    /// <summary>
    /// Сервис для выбора элементов в документе Revit
    /// </summary>
    public class SelectionService : ISelectionService
    {
        private readonly ExternalCommandData _commandData;

        /// <summary>
        /// Инициализирует новый экземпляр класса SelectionService
        /// </summary>
        /// <param name="commandData">Данные команды Revit</param>
        public SelectionService(ExternalCommandData commandData)
        {
            _commandData = commandData;
        }

        /// <summary>
        /// Выбирает элемент в документе Revit
        /// </summary>
        /// <returns>Выбранный элемент, если выбор отменен</returns>
        public FamilyInstance PickFamilyInstance()
        {
            try
            {
                Reference reference = _commandData.Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new FamilyInsanceSelectionFilter());
                FamilyInstance familyInstance = _commandData.Application.ActiveUIDocument.Document.GetElement(reference) as FamilyInstance;
                return familyInstance;
            }
            catch
            {
                return null;
            }
        }
    }
}
