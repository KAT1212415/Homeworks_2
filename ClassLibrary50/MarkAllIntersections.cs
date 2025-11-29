using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace SimpleCirclePlugin
{
    [Transaction(TransactionMode.Manual)]
    public class PlaceAnnotationsAtWallIntersections : IExternalCommand
    {
        private FamilySymbol _selectedFamilySymbol;
        private Level _selectedLevel;
        private View _selectedView;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // 1. Выбор уровня
                if (!SelectLevel(doc, "Выберите уровень для поиска пересечений стен"))
                {
                    TaskDialog.Show("Отмена", "Уровень не выбран");
                    return Result.Cancelled;
                }

                // 2. Выбор аннотационного семейства
                if (!SelectFamilySymbol(doc, "Выберите аннотационное семейство для маркировки пересечений"))
                {
                    TaskDialog.Show("Отмена", "Семейство не выбрано");
                    return Result.Cancelled;
                }

                // 3. Выбор вида
                if (!SelectView(doc, "Выберите вид для размещения аннотаций"))
                {
                    TaskDialog.Show("Отмена", "Вид не выбран");
                    return Result.Cancelled;
                }

                // 4. Получаем все стены на выбранном уровне
                List<Wall> walls = GetWallsOnLevel(doc, _selectedLevel);

                if (walls.Count < 2)
                {
                    TaskDialog.Show("Информация", $"На уровне '{_selectedLevel.Name}' найдено {walls.Count} стен. Для пересечений нужно минимум 2 стены.");
                    return Result.Succeeded;
                }

                // 5. Используем встроенную проверку пересечений Revit
                List<XYZ> intersectionPoints = FindWallIntersectionsUsingRevit(doc, walls);

                if (intersectionPoints.Count == 0)
                {
                    TaskDialog.Show("Информация", "Пересечения стен не найдены.");
                    return Result.Succeeded;
                }

                // 6. Размещаем аннотации в точках пересечений
                int placedCount = PlaceAnnotationsAtPoints(doc, intersectionPoints);

                TaskDialog.Show("Успех", $"Размещено {placedCount} аннотаций в точках пересечений стен.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Ошибка: {ex.Message}";
                TaskDialog.Show("Ошибка", message);
                return Result.Failed;
            }
        }

        private List<XYZ> FindWallIntersectionsUsingRevit(Document doc, List<Wall> walls)
        {
            List<XYZ> intersectionPoints = new List<XYZ>();
            HashSet<string> uniquePoints = new HashSet<string>();

            // Используем Solid-геометрию для точного определения пересечений
            foreach (Wall wall1 in walls)
            {
                foreach (Wall wall2 in walls)
                {
                    if (wall1.Id == wall2.Id) continue; // Пропускаем одну и ту же стену

                    try
                    {
                        // Получаем Solid-геометрию стен
                        IList<Solid> solids1 = GetWallSolids(wall1);
                        IList<Solid> solids2 = GetWallSolids(wall2);

                        foreach (Solid solid1 in solids1)
                        {
                            foreach (Solid solid2 in solids2)
                            {
                                // Используем встроенную проверку пересечений Solid'ов
                                Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Intersect);

                                if (intersection != null && intersection.Volume > 0.0001)
                                {
                                    // Получаем все точки пересечения из bounding box
                                    BoundingBoxXYZ bbox = intersection.GetBoundingBox();
                                    if (bbox != null)
                                    {
                                        XYZ centerPoint = (bbox.Min + bbox.Max) * 0.5;

                                        // Проверяем уникальность точки
                                        string pointKey = $"{Math.Round(centerPoint.X, 3)}_{Math.Round(centerPoint.Y, 3)}_{Math.Round(centerPoint.Z, 3)}";

                                        if (!uniquePoints.Contains(pointKey))
                                        {
                                            intersectionPoints.Add(centerPoint);
                                            uniquePoints.Add(pointKey);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Пропускаем ошибки при анализе пересечений
                        continue;
                    }
                }
            }

            return intersectionPoints;
        }

        private IList<Solid> GetWallSolids(Wall wall)
        {
            List<Solid> solids = new List<Solid>();

            try
            {
                // Получаем геометрию стены
                Options options = new Options();
                options.ComputeReferences = true;
                options.DetailLevel = ViewDetailLevel.Fine;

                GeometryElement geometry = wall.get_Geometry(options);

                foreach (GeometryObject geomObj in geometry)
                {
                    if (geomObj is Solid solid)
                    {
                        if (solid.Volume > 0) // Исключаем пустые Solid'ы
                        {
                            solids.Add(solid);
                        }
                    }
                    else if (geomObj is GeometryInstance geomInst)
                    {
                        // Обрабатываем экземпляры геометрии
                        GeometryElement instanceGeometry = geomInst.GetSymbolGeometry();
                        foreach (GeometryObject instGeomObj in instanceGeometry)
                        {
                            if (instGeomObj is Solid instSolid && instSolid.Volume > 0)
                            {
                                solids.Add(instSolid);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Возвращаем пустой список в случае ошибки
            }

            return solids;
        }

        // Альтернативный метод - используем LocationCurve для быстрого поиска пересечений
        private List<XYZ> FindWallIntersectionsUsingCurves(List<Wall> walls)
        {
            List<XYZ> intersectionPoints = new List<XYZ>();
            HashSet<string> uniquePoints = new HashSet<string>();

            for (int i = 0; i < walls.Count; i++)
            {
                for (int j = i + 1; j < walls.Count; j++)
                {
                    Curve curve1 = GetWallCenterLine(walls[i]);
                    Curve curve2 = GetWallCenterLine(walls[j]);

                    if (curve1 != null && curve2 != null)
                    {
                        try
                        {
                            // Используем встроенный метод Intersect
                            SetComparisonResult result = curve1.Intersect(curve2, out IntersectionResultArray results);

                            if (result == SetComparisonResult.Overlap && results != null)
                            {
                                foreach (IntersectionResult intersection in results)
                                {
                                    XYZ point = intersection.XYZPoint;
                                    string pointKey = $"{Math.Round(point.X, 3)}_{Math.Round(point.Y, 3)}_{Math.Round(point.Z, 3)}";

                                    if (!uniquePoints.Contains(pointKey))
                                    {
                                        intersectionPoints.Add(point);
                                        uniquePoints.Add(pointKey);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }

            return intersectionPoints;
        }

        // Существующие вспомогательные методы
        private bool SelectFamilySymbol(Document doc, string prompt)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var symbols = collector.OfClass(typeof(FamilySymbol))
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .Where(s => s.Category?.CategoryType == CategoryType.Annotation)
                .ToList();

            _selectedFamilySymbol = symbols.FirstOrDefault();
            return _selectedFamilySymbol != null;
        }

        private bool SelectLevel(Document doc, string prompt)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var levels = collector.OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            _selectedLevel = levels.FirstOrDefault();
            return _selectedLevel != null;
        }

        private bool SelectView(Document doc, string prompt)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var views = collector.OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate && v.CanBePrinted)
                .ToList();

            _selectedView = views.FirstOrDefault();
            return _selectedView != null;
        }

        private List<Wall> GetWallsOnLevel(Document doc, Level level)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            return collector.OfClass(typeof(Wall))
                .Cast<Wall>()
                .Where(w =>
                {
                    Parameter levelParam = w.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                    return levelParam?.AsElementId() == level.Id;
                })
                .ToList();
        }

        private Curve GetWallCenterLine(Wall wall)
        {
            return (wall.Location as LocationCurve)?.Curve;
        }

        private int PlaceAnnotationsAtPoints(Document doc, List<XYZ> points)
        {
            int placedCount = 0;

            using (Transaction transaction = new Transaction(doc, "Размещение аннотаций в пересечениях стен"))
            {
                transaction.Start();

                if (!_selectedFamilySymbol.IsActive)
                {
                    _selectedFamilySymbol.Activate();
                }

                foreach (XYZ point in points)
                {
                    try
                    {
                        FamilyInstance annotation = doc.Create.NewFamilyInstance(
                            point,
                            _selectedFamilySymbol,
                            _selectedView);

                        placedCount++;
                    }
                    catch (Exception)
                    {
                        // Пропускаем точки, где не удалось разместить аннотацию
                    }
                }

                transaction.Commit();
            }

            return placedCount;
        }
    }
}