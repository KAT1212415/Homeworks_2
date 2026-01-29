
﻿using Autodesk.Revit.DB;
using CSharpFunctionalExtensions;
using ClassLibrary15.Abstractions;
using ClassLibrary15.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibrary15.Services
{
    public class PlacementService : IPlacementService
    {
        private readonly Document _document;

        public PlacementService(Document document)
        {
            _document = document;
        }

        public Result Place(FurnitureType furnitureType, int count)
        {
            return Validate(count)
                .Bind(() => FindFamily(furnitureType))
                .Bind(s => PlaceInstances(s, count));
        }

        private Result Validate(int count)
        {
            if (count < 0)
                return Result.Failure("Количество должно быть больше 0");
            return Result.Success();
        }

        private Result<FamilySymbol> FindFamily(FurnitureType furnitureType)
        {
              string searchString = string.Empty;  // Что ищем
    bool searchInName = false;           // Где ищем: true=в Name, false=в FamilyName
    BuiltInCategory category = BuiltInCategory.OST_Furniture;
    
    switch (furnitureType)
    {
        case FurnitureType.Table:
            searchString = "Стол";
            searchInName = false;  // Ищем в FamilyName
            break;
        case FurnitureType.Chair:
            searchString = "Стул";
            searchInName = false;  // Ищем в FamilyName
            break;
        case FurnitureType.Cabinet:
            searchString = "Шкаф";
            searchInName = false;  // Ищем в FamilyName
            break;
        case FurnitureType.Trees:
            searchString = "Береза";
            searchInName = true;   // Ищем в Name
            category = BuiltInCategory.OST_Planting;
            break;
    }

    // ЕДИНАЯ ФОРМУЛА
    FamilySymbol familySymbol = new FilteredElementCollector(_document)
        .OfCategory(category)
        .OfClass(typeof(FamilySymbol))
        .Cast<FamilySymbol>()
        .Where(x => searchInName 
            ? x.Name.Contains(searchString)  // Если true: ищем в Name
            : x.FamilyName.Contains(searchString)) // Если false: ищем в FamilyName
        .FirstOrDefault();

    if (familySymbol == null)
        return Result.Failure<FamilySymbol>($"Не найден типоразмер '{searchString}' для размещения");

    return familySymbol;
}





  
        private Result PlaceInstances(FamilySymbol familySymbol, int count)
        {
            try
            {
                //double step = UnitUtils.ConvertToInternalUnits(2, DisplayUnitType.DUT_METERS);
                //var points = new List<XYZ>();
                //for (int i = 0; i < count; i++)
                //{
                //    points.Add(new XYZ(i * step, 0, 0));
                //}

                // Шаг между элементами в сетке (2 метра)
                double step = UnitUtils.ConvertToInternalUnits(2, DisplayUnitType.DUT_METERS);
                var points = new List<XYZ>();

                // РАЗМЕЩЕНИЕ ПО КВАДРАТНОЙ СЕТКЕ

                // 1. Определяем размер сетки
                // columns = количество столбцов (округляем квадратный корень вверх)
                int columns = (int)Math.Ceiling(Math.Sqrt(count));

                // rows = количество строк (делим общее количество на столбцы и округляем вверх)
                int rows = (int)Math.Ceiling((double)count / columns);

                // 2. Центрируем сетку (чтобы она была симметричной относительно начала координат)
                double offsetX = (columns - 1) * step / 2;  // Смещение по X для центрирования
                double offsetY = (rows - 1) * step / 2;     // Смещение по Y для центрирования

                // 3. Создаем точки в сетке
                for (int i = 0; i < count; i++)
                {
                    // Определяем строку и столбец для текущего элемента
                    int row = i / columns;     // Целочисленное деление (номер строки)
                    int col = i % columns;     // Остаток от деления (номер столбца)

                    // Вычисляем координаты с центрированием
                    double x = col * step - offsetX;
                    double y = row * step - offsetY;

                    points.Add(new XYZ(x, y, 0));
                }








                    var level = new FilteredElementCollector(_document)
                    .OfClass(typeof(Level))
                    .OfType<Level>()
                    .OrderBy(l => l.Elevation)
                    .FirstOrDefault();

                    if (level == null)
                        return Result.Failure("Не удалось определить уровень для размещения");

                    using (Transaction transaction = new Transaction(_document, "Размещение мебели"))
                    {
                        transaction.Start();

                        if (!familySymbol.IsActive)
                        {
                            familySymbol.Activate();
                        }

                        foreach (var point in points)
                        {
                            _document.Create.NewFamilyInstance(
                                point,
                                familySymbol,
                                level,
                                Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                        }
                        transaction.Commit();
                    }
                    return Result.Success();
                }
            
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }
        }
    }
}