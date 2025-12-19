

using Autodesk.Revit.DB;

namespace ClassLibrary9.Abstractions
{
    /// <summary>
    /// Интерфейс сервиса для выбора элементов в Revit
    /// </summary>
    public interface ISelectionServices
    {
        /// <summary>
        /// Выбирает проем (дверь или окно) в документе Revit
        /// </summary>
        /// <returns>Выбранный проем или null, если выбор отменен</returns>
        Wall PickOpening();
    }
}
