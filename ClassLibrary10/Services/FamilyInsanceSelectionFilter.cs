

using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace ClassLibrary10.Services
{
    internal class FamilyInsanceSelectionFilter : ISelectionFilter
    {
        /// <summary>
        /// Определяет, разрешен ли выбор указанного элемента
        /// </summary>
        /// <param name="elem">Элемент для проверки</param>
        /// <returns>true, если элемент является FamilyInstance; иначе false</returns>
        public bool AllowElement(Element elem)
        {
            if (!(elem is FamilyInstance fi))
                return false;
            return true;
        }

        /// <summary>
        /// Определяет, разрешен ли выбор указанной ссылки
        /// </summary>
        /// <param name="reference">Ссылка на элемент</param>
        /// <param name="position">Позиция выбора</param>
        /// <returns>false - ссылки не разрешены</returns>
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
