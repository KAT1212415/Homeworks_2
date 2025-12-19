using Autodesk.Revit.DB;
using ClassLibrary9.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary9.Abstractions
{
    /// <summary>
    /// Интерфейс сервиса для работы с геометрией проемов
    /// </summary>
    public interface IGeometryService
    {
        /// <summary>
        /// Получает информацию о проеме
        /// </summary>
        /// <param name="opening">Экземпляр семейства проема</param>
        /// <param name="limit">Ограничение по расстоянию</param>
        /// <returns>Информация о проеме</returns>
        OpeningInfo GetOpeningInfo(Wall opening, double limit);
    }
}
