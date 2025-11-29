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
                // 1. Выбор аннотационного семейства
                if (!SelectFamilySymbol(doc, "Выберите аннотационное семейство для пересечений стен"))
                {
                    TaskDialog.Show("Отмена", "Семейство не выбрано");
                    return Result.Cancelled;
                }

                // 2. Выбор уровня
                if (!SelectLevel(doc, "Выберите уровень для поиска пересечений стен"))
                {
                    TaskDialog.Show("Отмена", "Уровень не выбран");
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

                // 5. Находим все пересечения, исключая системные соединения концов
                List<XYZ> intersectionPoints = FindRealWallIntersections(walls, doc);

                if (intersectionPoints.Count == 0)
                {
                    TaskDialog.Show("Информация", $"Реальные пересечения стен не найдены на уровне '{_selectedLevel.Name}'");
                    return Result.Succeeded;
                }

                // 6. Размещаем аннотации в точках пересечения
                int placedCount = PlaceAnnotations(doc, intersectionPoints, _selectedFamilySymbol, _selectedLevel, _selectedView);

                TaskDialog.Show("Успех",
                    $"✅ Найдено {intersectionPoints.Count} реальных пересечений стен (исключены системные соединения)\n" +
                    $"📐 Размещено {placedCount} аннотаций\n" +
                    $"🏗️ Уровень: '{_selectedLevel.Name}'");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Не удалось разместить аннотации: {ex.Message}");
                return Result.Failed;
            }
        }

        private bool SelectFamilySymbol(Document doc, string prompt)
        {
            try
            {
                // Получаем все аннотационные семейства
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_GenericAnnotation);

                List<FamilySymbol> familySymbols = collector
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.IsActive)
                    .ToList();

                if (familySymbols.Count == 0)
                {
                    TaskDialog.Show("Ошибка", "В проекте не найдено аннотационных семейств");
                    return false;
                }

                // Создаем диалог выбора
                var dialog = new TaskDialog("Выбор семейства");
                dialog.MainInstruction = prompt;
                dialog.CommonButtons = TaskDialogCommonButtons.Cancel;

                foreach (var familySymbol in familySymbols)
                {
                    string familyName = familySymbol.Family.Name;
                    string typeName = familySymbol.Name;
                    dialog.AddCommandLink(
                        TaskDialogCommandLinkId.CommandLink1 + familySymbols.IndexOf(familySymbol),
                        $"{familyName} - {typeName}");
                }

                TaskDialogResult result = dialog.Show();

                if (result == TaskDialogResult.Cancel)
                    return false;

                int selectedIndex = (int)result - (int)TaskDialogCommandLinkId.CommandLink1;
                if (selectedIndex >= 0 && selectedIndex < familySymbols.Count)
                {
                    _selectedFamilySymbol = familySymbols[selectedIndex];
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Ошибка выбора семейства: {ex.Message}");
                return false;
            }
        }

        private bool SelectLevel(Document doc, string prompt)
        {
            try
            {
                // Получаем все уровни
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level));

                List<Level> levels = collector
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                if (levels.Count == 0)
                {
                    TaskDialog.Show("Ошибка", "В проекте не найдено уровней");
                    return false;
                }

                // Создаем диалог выбора
                var dialog = new TaskDialog("Выбор уровня");
                dialog.MainInstruction = prompt;
                dialog.CommonButtons = TaskDialogCommonButtons.Cancel;

                foreach (var level in levels)
                {
                    dialog.AddCommandLink(
                        TaskDialogCommandLinkId.CommandLink1 + levels.IndexOf(level),
                        $"{level.Name} (Высота: {Math.Round(level.Elevation * 304.8)} мм)");
                }

                TaskDialogResult result = dialog.Show();

                if (result == TaskDialogResult.Cancel)
                    return false;

                int selectedIndex = (int)result - (int)TaskDialogCommandLinkId.CommandLink1;
                if (selectedIndex >= 0 && selectedIndex < levels.Count)
                {
                    _selectedLevel = levels[selectedIndex];
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Ошибка выбора уровня: {ex.Message}");
                return false;
            }
        }

        private bool SelectView(Document doc, string prompt)
        {
            try
            {
                // Получаем все виды, где можно размещать аннотации
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(View));

                List<View> views = collector
                    .Cast<View>()
                    .Where(v => !v.IsTemplate && v.CanBePrinted && v.ViewType != ViewType.ThreeD)
                    .OrderBy(v => v.Name)
                    .ToList();

                if (views.Count == 0)
                {
                    TaskDialog.Show("Ошибка", "В проекте не найдено подходящих видов");
                    return false;
                }

                // Создаем диалог выбора
                var dialog = new TaskDialog("Выбор вида");
                dialog.MainInstruction = prompt;
                dialog.CommonButtons = TaskDialogCommonButtons.Cancel;

                foreach (var view in views)
                {
                    dialog.AddCommandLink(
                        TaskDialogCommandLinkId.CommandLink1 + views.IndexOf(view),
                        $"{view.Name} ({view.ViewType})");
                }

                TaskDialogResult result = dialog.Show();

                if (result == TaskDialogResult.Cancel)
                    return false;

                int selectedIndex = (int)result - (int)TaskDialogCommandLinkId.CommandLink1;
                if (selectedIndex >= 0 && selectedIndex < views.Count)
                {
                    _selectedView = views[selectedIndex];
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Ошибка выбора вида: {ex.Message}");
                return false;
            }
        }

        private List<Wall> GetWallsOnLevel(Document doc, Level level)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType();

            List<Wall> walls = new List<Wall>();
            foreach (Element element in collector)
            {
                Wall wall = element as Wall;
                if (wall != null)
                {
                    Parameter levelParam = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                    if (levelParam != null && levelParam.AsElementId() == level.Id)
                    {
                        walls.Add(wall);
                    }
                }
            }
            return walls;
        }

        private List<XYZ> FindRealWallIntersections(List<Wall> walls, Document doc)
        {
            List<XYZ> intersections = new List<XYZ>();

            for (int i = 0; i < walls.Count - 1; i++)
            {
                for (int j = i + 1; j < walls.Count; j++)
                {
                    List<XYZ> wallIntersections = FindWallIntersections(walls[i], walls[j], doc);

                    // Фильтруем точки: оставляем только реальные пересечения, исключаем системные соединения
                    foreach (XYZ intersection in wallIntersections)
                    {
                        if (!IsSystemWallJoin(walls[i], walls[j], intersection, doc))
                        {
                            XYZ correctedPoint = new XYZ(intersection.X, intersection.Y, _selectedLevel.Elevation);

                            // Проверяем, что точка не дублируется
                            if (!intersections.Any(p => p.DistanceTo(correctedPoint) < 0.01))
                            {
                                intersections.Add(correctedPoint);
                            }
                        }
                    }
                }
            }

            return intersections;
        }

        private bool IsSystemWallJoin(Wall wall1, Wall wall2, XYZ intersectionPoint, Document doc)
        {
            try
            {
                // Получаем конечные точки обеих стен
                LocationCurve loc1 = wall1.Location as LocationCurve;
                LocationCurve loc2 = wall2.Location as LocationCurve;

                if (loc1 == null || loc2 == null) return false;

                Curve curve1 = loc1.Curve;
                Curve curve2 = loc2.Curve;

                // Проверяем, находится ли точка пересечения близко к концам стен
                double tolerance = 0.01; // 1 см допуск

                bool isNearWall1End = IsPointNearCurveEnd(curve1, intersectionPoint, tolerance);
                bool isNearWall2End = IsPointNearCurveEnd(curve2, intersectionPoint, tolerance);

                // Если точка находится близко к концам ОБЕИХ стен - это вероятно системное соединение
                return isNearWall1End && isNearWall2End;
            }
            catch
            {
                return false;
            }
        }

        private bool IsPointNearCurveEnd(Curve curve, XYZ point, double tolerance)
        {
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);

            double distToStart = point.DistanceTo(startPoint);
            double distToEnd = point.DistanceTo(endPoint);

            // Точка считается близко к концу, если расстояние меньше допуска
            return distToStart < tolerance || distToEnd < tolerance;
        }

        private List<XYZ> FindWallIntersections(Wall wall1, Wall wall2, Document doc)
        {
            List<XYZ> intersections = new List<XYZ>();

            try
            {
                LocationCurve loc1 = wall1.Location as LocationCurve;
                LocationCurve loc2 = wall2.Location as LocationCurve;

                if (loc1 == null || loc2 == null) return intersections;

                Curve curve1 = loc1.Curve;
                Curve curve2 = loc2.Curve;

                if (curve1 is Line && curve2 is Line)
                {
                    Line line1 = curve1 as Line;
                    Line line2 = curve2 as Line;

                    IntersectionResultArray results;
                    SetComparisonResult result = line1.Intersect(line2, out results);

                    if (result == SetComparisonResult.Overlap && results != null)
                    {
                        foreach (IntersectionResult intersection in results)
                        {
                            intersections.Add(intersection.XYZPoint);
                        }
                    }
                    else if (result == SetComparisonResult.Disjoint)
                    {
                        XYZ extendedIntersection = FindExtendedIntersection(line1, line2);
                        if (extendedIntersection != null)
                        {
                            intersections.Add(extendedIntersection);
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки для отдельных пар
            }

            return intersections;
        }

        private XYZ FindExtendedIntersection(Line line1, Line line2)
        {
            try
            {
                XYZ direction1 = line1.Direction;
                XYZ direction2 = line2.Direction;

                XYZ point1 = line1.GetEndPoint(0);
                XYZ point2 = line2.GetEndPoint(0);

                XYZ v = point2 - point1;
                XYZ cross12 = direction1.CrossProduct(direction2);
                XYZ cross1v = direction1.CrossProduct(v);
                XYZ cross2v = direction2.CrossProduct(v);

                double denominator = cross12.DotProduct(cross12);

                if (Math.Abs(denominator) > 1e-10)
                {
                    double t = cross2v.DotProduct(cross12) / denominator;

                    if (Math.Abs(t) < 10.0)
                    {
                        XYZ intersection = point1 + t * direction1;

                        double dist1 = line1.Distance(intersection);
                        double dist2 = line2.Distance(intersection);

                        if (dist1 < 0.1 && dist2 < 0.1)
                        {
                            return intersection;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private int PlaceAnnotations(Document doc, List<XYZ> points, FamilySymbol familySymbol, Level level, View view)
        {
            int placedCount = 0;

            using (Transaction trans = new Transaction(doc, "Place Wall Intersection Annotations"))
            {
                trans.Start();

                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                }

                foreach (XYZ point in points)
                {
                    try
                    {
                        FamilyInstance annotation = doc.Create.NewFamilyInstance(
                            point,
                            familySymbol,
                            view);

                        Parameter levelParam = annotation.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                        if (levelParam != null && !levelParam.IsReadOnly)
                        {
                            levelParam.Set(level.Id);
                        }

                        placedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка размещения аннотации: {ex.Message}");
                    }
                }

                trans.Commit();
            }

            return placedCount;
        }
    }
}