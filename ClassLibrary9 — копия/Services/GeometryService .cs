using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClassLibrary9.Abstractions;
using ClassLibrary9.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xaml;

namespace ClassLibrary9.Services
{
    /// <summary>
    /// Сервис для работы с геометрией проемов в стенах
    /// </summary>
    public class GeometryService : IGeometryService
    {
        /// <summary>
        /// Получает информацию о проеме, включая расстояние от верха проема до верха стены
        /// </summary>
        /// <param name="opening">Экземпляр семейства проема</param>
        /// <param name="limit">Ограничение по расстоянию в миллиметрах</param>
        /// <returns>Информация о проеме</returns>
        public OpeningInfo GetOpeningInfo(Wall wall, double limit)
        {
            ////double currentDistance = GetCurrentDistance(opening);
            ///
            
            double length = UnitUtils.ConvertFromInternalUnits(
           wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble(),
           DisplayUnitType.DUT_METERS);
            double area = UnitUtils.ConvertFromInternalUnits(
           wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble(),
           DisplayUnitType.DUT_SQUARE_METERS);
            double volume = UnitUtils.ConvertFromInternalUnits(
           wall.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED).AsDouble(),
           DisplayUnitType.DUT_CUBIC_METERS);
            double height = UnitUtils.ConvertFromInternalUnits(
           wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble(),
           DisplayUnitType.DUT_METERS);
            // double thickness = UnitUtils.ConvertFromInternalUnits(
            //wall.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM).AsDouble(),
            //DisplayUnitType.DUT_METERS);

            double thickness = volume/ area; // Значение по умолчанию 300 мм = 0.3 метра

            //try
            //{
            //    // Пробуем получить толщину разными способами
            //    Parameter thicknessParam = wall.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);

            //    if (thicknessParam != null && thicknessParam.HasValue)
            //    {
            //        thickness = UnitUtils.ConvertFromInternalUnits(
            //            thicknessParam.AsDouble(),
            //            DisplayUnitType.DUT_METERS);
            //    }
            //    else
            //    {
            //        thickness = 0.3;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    // Логирование ошибки (опционально)
            //    // System.Diagnostics.Debug.WriteLine($"Ошибка получения толщины: {ex.Message}");

            //    // Присваиваем дефолтное значение 300 мм
            //    thickness = 0.3;
            //}
            ////try
            ////{
            ////    // 1. Показываем информацию о стене
            ////    TaskDialog.Show("Отладка",
            ////        $"Имя стены: {wall.Name}\n" +
            ////        $"Тип стены: {wall.WallType?.Name}\n" +
            ////        $"Kind: {wall.WallType?.Kind}");

            ////    // 2. Получаем параметр
            ////    Parameter thicknessParam = wall.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);

            ////    // 3. Проверяем что получили
            ////    if (thicknessParam == null)
            ////    {
            ////        TaskDialog.Show("Отладка", "Параметр WALL_ATTR_WIDTH_PARAM НЕ НАЙДЕН!");
            ////    }
            ////    else if (!thicknessParam.HasValue)
            ////    {
            ////        TaskDialog.Show("Отладка", "Параметр WALL_ATTR_WIDTH_PARAM не имеет значения!");
            ////    }
            ////    else
            ////    {
            ////        // 4. Показываем сырое значение
            ////        double rawValue = thicknessParam.AsDouble();
            ////        TaskDialog.Show("Отладка",
            ////            $"Сырое значение: {rawValue}\n" +
            ////            $"StorageType: {thicknessParam.StorageType}");

            ////        // 5. Конвертируем
            ////        thickness = UnitUtils.ConvertFromInternalUnits(
            ////            rawValue,
            ////            DisplayUnitType.DUT_METERS);

            ////        TaskDialog.Show("Отладка", $"Толщина в метрах: {thickness}");
            ////    }
            ////}
            ////catch (Exception ex)
            ////{
            ////    TaskDialog.Show("Ошибка", ex.Message);
            ////    thickness = 0.3;
            ////}



            return new OpeningInfo()
            {
                WallName = wall.WallType?.FamilyName ?? "Без семейства",
                WallType = wall.WallType?.Name ?? "Без типа",
                Lenght = length,
                Height = height,
                Thickness = thickness,
                Volume = volume,
                Area = area,
                IsCorrect = thickness < limit

                //IsCorrect = currentDistance < limit
            };
        }

        //private double GetCurrentDistance(FamilyInstance opening)
        //{
        //    double maxZOpening = GetMaxZ(opening);
        //    double maxZHost = GetMaxZ(opening.Host);

        //    return UnitUtils.ConvertFromInternalUnits(maxZHost - maxZOpening, DisplayUnitType.DUT_MILLIMETERS);
        //}

        //private double GetMaxZ(Element element)
        //{
        //    var box = element.get_BoundingBox(null);
        //    return box.Max.Z;
        //}
    }
}
