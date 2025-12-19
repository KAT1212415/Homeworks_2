using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace ClassLibrary9.Services
{
    /// <summary>
    /// Фильтр выбора, позволяющий выбирать только двери и окна
    /// </summary>
    internal class OpeningSelectionFilter : ISelectionFilter
    {
        /// <summary>
        /// Определяет, разрешен ли выбор указанного элемента
        /// </summary>
        /// <param name="elem">Элемент для проверки</param>
        /// <returns>true, если элемент является дверью или окном; иначе false</returns>
        public bool AllowElement(Element elem)
        {
            {
                // Проверяем, что элемент принадлежит категории стен
                return elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Walls;
            }
           
            
        }

        /// <summary>
        /// Определяет, разрешен ли выбор указанной ссылки
        /// </summary>
        /// <param name="reference">Ссылка на элемент</param>
        /// <param name="position">Позиция выбора</param>
        /// <returns>false - ссылки не разрешены</returns>
        public bool AllowReference(Autodesk.Revit.DB.Reference reference, XYZ position) => false;
                
    }
}
