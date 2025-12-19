using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ClassLibrary9.Abstractions;

namespace ClassLibrary9.Services
{
    /// <summary>
    /// Сервис для выбора элементов в документе Revit
    /// </summary>
    public class SelectionService : ISelectionServices
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
        /// Выбирает проем (дверь или окно) в документе Revit
        /// </summary>
        /// <returns>Выбранный проем или null, если выбор отменен</returns>
        public Wall PickOpening()
        {
            try
            {
                Autodesk.Revit.DB.Reference reference = _commandData.Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new OpeningSelectionFilter());
                Wall wall = _commandData.Application.ActiveUIDocument.Document.GetElement(reference) as Wall;
                return wall;
            }
            catch
            {
                return null;
            }
        }
    }
}
