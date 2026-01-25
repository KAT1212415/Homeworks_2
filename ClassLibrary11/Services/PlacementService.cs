using Autodesk.Revit.DB;
using ClassLibrary11.Abstractions;
using ClassLibrary11.Models;
using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassLibrary11.Services
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
            string furnutureName = string.Empty;
            switch (furnitureType)
            {
                case FurnitureType.Table:
                    furnutureName = "Стол";
                    break;
                case FurnitureType.Chair:
                    furnutureName = "Стул";
                    break;
                case FurnitureType.Cabinet:
                    furnutureName = "Шкаф";
                    break;
            }

            FamilySymbol familySymbol = new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_Furniture)
                .OfClass(typeof(FamilySymbol))
                .OfType<FamilySymbol>()
                .Where(x => x.FamilyName.Contains(furnutureName))
                .FirstOrDefault();

            if (familySymbol == null)
                return Result.Failure<FamilySymbol>("Не найден типоразмер для размещения");

            return familySymbol;
        }

        private Result PlaceInstances(FamilySymbol familySymbol, int count)
        {
            try
            {
                double step = UnitUtils.ConvertToInternalUnits(2, DisplayUnitType.DUT_METERS);
                var points = new List<XYZ>();
                for (int i = 0; i < count; i++)
                {
                    points.Add(new XYZ(i * step, 0, 0));
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