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
    public class PlaceAnnotationsAtIntersections : IExternalCommand
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
                if (!SelectFamilySymbol(doc, "Выберите аннотационное семейство для точек пересечения"))
                {
                    TaskDialog.Show("Отмена", "Семейство не выбрано");
                    return Result.Cancelled;
                }

                // 2. Выбор уровня
                if (!SelectLevel(doc, "Выберите уровень для поиска пересечений"))
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

                // 4. Получаем все элементы на выбранном уровне
                List<Wall> walls = GetWallsOnLevel(doc, _selectedLevel);
                List<FamilyInstance> columns = GetColumnsOnLevel(doc, _selectedLevel);
                List<FamilyInstance> structuralColumns = GetStructuralColumnsOnLevel(doc, _selectedLevel);

                TaskDialog.Show("Поиск элементов",
                    $"Найдено на уровне '{_selectedLevel.Name}':\n" +
                    $"• Стен: {walls.Count}\n" +
                    $"• Колонн: {columns.Count}\n" +
                    $"• Несущих колонн: {structuralColumns.Count}");

                if (walls.Count + columns.Count + structuralColumns.Count < 2)
                {
                    TaskDialog.Show("Информация",
                        "Для пересечений нужно минимум 2 элемента.");
                    return Result.Succeeded;
                }

                // 5. Находим все пересечения
                List<XYZ> intersectionPoints = FindAllIntersections(walls, columns, structuralColumns, doc);

                if (intersectionPoints.Count == 0)
                {
                    TaskDialog.Show("Информация", $"Пересечения не найдены на уровне '{_selectedLevel.Name}'");
                    return Result.Succeeded;
                }

                // 6. Размещаем аннотации в точках пересечения
                int placedCount = PlaceAnnotations(doc, intersectionPoints, _selectedFamilySymbol, _selectedLevel, _selectedView);

                TaskDialog.Show("Успех",
                    $"✅ Найдено {intersectionPoints.Count} точек пересечения\n" +
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

        private List<FamilyInstance> GetColumnsOnLevel(Document doc, Level level)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Columns)
                .WhereElementIsNotElementType();

            List<FamilyInstance> columns = new List<FamilyInstance>();
            foreach (Element element in collector)
            {
                FamilyInstance column = element as FamilyInstance;
                if (column != null)
                {
                    Parameter levelParam = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
                    if (levelParam != null && levelParam.AsElementId() == level.Id)
                    {
                        columns.Add(column);
                    }
                }
            }
            return columns;
        }

        private List<FamilyInstance> GetStructuralColumnsOnLevel(Document doc, Level level)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType();

            List<FamilyInstance> structuralColumns = new List<FamilyInstance>();
            foreach (Element element in collector)
            {
                FamilyInstance column = element as FamilyInstance;
                if (column != null)
                {
                    Parameter levelParam = column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
                    if (levelParam != null && levelParam.AsElementId() == level.Id)
                    {
                        structuralColumns.Add(column);
                    }
                }
            }
            return structuralColumns;
        }

        private List<XYZ> FindAllIntersections(List<Wall> walls, List<FamilyInstance> columns,
            List<FamilyInstance> structuralColumns, Document doc)
        {
            List<XYZ> intersections = new List<XYZ>();

            // 1. Пересечения стен между собой
            for (int i = 0; i < walls.Count - 1; i++)
            {
                for (int j = i + 1; j < walls.Count; j++)
                {
                    List<XYZ> wallIntersections = FindWallToWallIntersections(walls[i], walls[j], doc);
                    AddUniquePoints(intersections, wallIntersections);
                }
            }

            // 2. Пересечения стен с архитектурными колоннами
            foreach (Wall wall in walls)
            {
                foreach (FamilyInstance column in columns)
                {
                    List<XYZ> intersectionsWC = FindWallToColumnIntersections(wall, column, doc, "Архитектурная колонна");
                    AddUniquePoints(intersections, intersectionsWC);
                }
            }

            // 3. Пересечения стен с несущими колоннами
            foreach (Wall wall in walls)
            {
                foreach (FamilyInstance structuralColumn in structuralColumns)
                {
                    List<XYZ> intersectionsWSC = FindWallToStructuralColumnIntersections(wall, structuralColumn, doc);
                    AddUniquePoints(intersections, intersectionsWSC);
                }
            }

            return intersections;
        }

        private void AddUniquePoints(List<XYZ> mainList, List<XYZ> newPoints)
        {
            foreach (XYZ point in newPoints)
            {
                // Корректируем Z-координату на высоту уровня
                XYZ correctedPoint = new XYZ(point.X, point.Y, _selectedLevel.Elevation);

                // Проверяем, что точка не дублируется (в пределах 10 мм)
                if (!mainList.Any(p => p.DistanceTo(correctedPoint) < 0.01))
                {
                    mainList.Add(correctedPoint);
                }
            }
        }

        private List<XYZ> FindWallToWallIntersections(Wall wall1, Wall wall2, Document doc)
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

        private List<XYZ> FindWallToStructuralColumnIntersections(Wall wall, FamilyInstance structuralColumn, Document doc)
        {
            List<XYZ> intersections = new List<XYZ>();

            try
            {
                // Получаем линию стены
                LocationCurve wallLocation = wall.Location as LocationCurve;
                if (wallLocation == null) return intersections;

                Curve wallCurve = wallLocation.Curve;
                if (!(wallCurve is Line)) return intersections;

                Line wallLine = wallCurve as Line;

                // Метод 1: Анализ геометрии несущей колонны
                List<XYZ> geomIntersections = FindIntersectionsWithGeometry(wallLine, structuralColumn, doc, "Несущая колонна");
                intersections.AddRange(geomIntersections);

                // Метод 2: Анализ Location Point для колонн
                if (intersections.Count == 0)
                {
                    LocationPoint columnLocation = structuralColumn.Location as LocationPoint;
                    if (columnLocation != null)
                    {
                        XYZ columnCenter = columnLocation.Point;
                        XYZ closestPoint = wallLine.Project(columnCenter).XYZPoint;

                        // Проверяем расстояние от стены до центра колонны
                        double distance = closestPoint.DistanceTo(columnCenter);

                        // Получаем размеры колонны
                        double columnSize = GetColumnSize(structuralColumn);

                        if (distance <= columnSize * 1.2) // С запасом 20%
                        {
                            intersections.Add(closestPoint);
                        }
                    }
                }

                // Метод 3: Анализ BoundingBox
                if (intersections.Count == 0)
                {
                    BoundingBoxXYZ columnBBox = structuralColumn.get_BoundingBox(null);
                    if (columnBBox != null)
                    {
                        List<XYZ> bboxIntersections = FindIntersectionsWithBoundingBox(wallLine, columnBBox);
                        intersections.AddRange(bboxIntersections);
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка поиска пересечения стены с несущей колонной: {ex.Message}");
            }

            return intersections;
        }

        private List<XYZ> FindWallToColumnIntersections(Wall wall, FamilyInstance column, Document doc, string columnType)
        {
            List<XYZ> intersections = new List<XYZ>();

            try
            {
                // Получаем линию стены
                LocationCurve wallLocation = wall.Location as LocationCurve;
                if (wallLocation == null) return intersections;

                Curve wallCurve = wallLocation.Curve;
                if (!(wallCurve is Line)) return intersections;

                Line wallLine = wallCurve as Line;

                // Метод 1: Анализ геометрии
                List<XYZ> geomIntersections = FindIntersectionsWithGeometry(wallLine, column, doc, columnType);
                intersections.AddRange(geomIntersections);

                // Метод 2: Анализ Location Point
                if (intersections.Count == 0)
                {
                    LocationPoint columnLocation = column.Location as LocationPoint;
                    if (columnLocation != null)
                    {
                        XYZ columnCenter = columnLocation.Point;
                        XYZ closestPoint = wallLine.Project(columnCenter).XYZPoint;

                        double distance = closestPoint.DistanceTo(columnCenter);
                        double columnSize = GetColumnSize(column);

                        if (distance <= columnSize * 1.2)
                        {
                            intersections.Add(closestPoint);
                        }
                    }
                }

                // Метод 3: Анализ BoundingBox
                if (intersections.Count == 0)
                {
                    BoundingBoxXYZ columnBBox = column.get_BoundingBox(null);
                    if (columnBBox != null)
                    {
                        List<XYZ> bboxIntersections = FindIntersectionsWithBoundingBox(wallLine, columnBBox);
                        intersections.AddRange(bboxIntersections);
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка поиска пересечения стены с {columnType}: {ex.Message}");
            }

            return intersections;
        }

        private List<XYZ> FindIntersectionsWithGeometry(Line wallLine, FamilyInstance column, Document doc, string columnType)
        {
            List<XYZ> intersections = new List<XYZ>();

            try
            {
                Options geomOptions = new Options();
                geomOptions.DetailLevel = ViewDetailLevel.Fine;
                geomOptions.ComputeReferences = true;

                GeometryElement columnGeometry = column.get_Geometry(geomOptions);
                if (columnGeometry == null) return intersections;

                foreach (GeometryObject geomObj in columnGeometry)
                {
                    if (geomObj is Solid solid && solid.Volume > 0)
                    {
                        // Проверяем пересечение с гранями
                        FaceArray faces = solid.Faces;
                        foreach (Face face in faces)
                        {
                            IntersectionResultArray faceResults;
                            SetComparisonResult faceResult = face.Intersect(wallLine, out faceResults);

                            if (faceResult == SetComparisonResult.Overlap && faceResults != null)
                            {
                                foreach (IntersectionResult faceIntersection in faceResults)
                                {
                                    intersections.Add(faceIntersection.XYZPoint);
                                }
                            }
                        }

                        // Дополнительно: проверяем пересечение с ребрами
                        EdgeArray edges = solid.Edges;
                        foreach (Edge edge in edges)
                        {
                            Curve edgeCurve = edge.AsCurve();
                            if (edgeCurve != null)
                            {
                                IntersectionResultArray edgeResults;
                                SetComparisonResult edgeResult = wallLine.Intersect(edgeCurve, out edgeResults);

                                if (edgeResult == SetComparisonResult.Overlap && edgeResults != null)
                                {
                                    foreach (IntersectionResult edgeIntersection in edgeResults)
                                    {
                                        intersections.Add(edgeIntersection.XYZPoint);
                                    }
                                }
                            }
                        }
                    }
                    else if (geomObj is GeometryInstance geomInstance)
                    {
                        // Обрабатываем вложенную геометрию
                        GeometryElement instanceGeometry = geomInstance.GetSymbolGeometry();
                        foreach (GeometryObject instGeomObj in instanceGeometry)
                        {
                            if (instGeomObj is Solid instSolid && instSolid.Volume > 0)
                            {
                                FaceArray instFaces = instSolid.Faces;
                                foreach (Face instFace in instFaces)
                                {
                                    IntersectionResultArray instFaceResults;
                                    SetComparisonResult instFaceResult = instFace.Intersect(wallLine, out instFaceResults);

                                    if (instFaceResult == SetComparisonResult.Overlap && instFaceResults != null)
                                    {
                                        foreach (IntersectionResult instFaceIntersection in instFaceResults)
                                        {
                                            intersections.Add(instFaceIntersection.XYZPoint);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка анализа геометрии {columnType}: {ex.Message}");
            }

            return intersections;
        }

        private double GetColumnSize(FamilyInstance column)
        {
            try
            {
                // Пытаемся получить размеры колонны из параметров
                Parameter widthParam = column.get_Parameter(BuiltInParameter.FAMILY_WIDTH_PARAM);
                Parameter depthParam = column.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM);

                double width = widthParam?.AsDouble() ?? 0.3; // Значение по умолчанию 300мм
                double depth = depthParam?.AsDouble() ?? 0.3;

                return Math.Max(width, depth);
            }
            catch
            {
                return 0.3; // Значение по умолчанию
            }
        }

        private List<XYZ> FindIntersectionsWithBoundingBox(Line wallLine, BoundingBoxXYZ bbox)
        {
            List<XYZ> intersections = new List<XYZ>();

            try
            {
                // Создаем линии контура BoundingBox
                List<Line> bboxLines = CreateBoundingBoxLines(bbox);

                foreach (Line bboxLine in bboxLines)
                {
                    IntersectionResultArray results;
                    SetComparisonResult result = wallLine.Intersect(bboxLine, out results);

                    if (result == SetComparisonResult.Overlap && results != null)
                    {
                        foreach (IntersectionResult intersection in results)
                        {
                            intersections.Add(intersection.XYZPoint);
                        }
                    }
                }

                // Также проверяем проекцию центра на стену
                XYZ bboxCenter = (bbox.Min + bbox.Max) / 2.0;
                XYZ projectedPoint = wallLine.Project(bboxCenter).XYZPoint;

                // Проверяем, что проекция находится внутри BoundingBox
                if (IsPointInsideBoundingBox(projectedPoint, bbox))
                {
                    intersections.Add(projectedPoint);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка анализа BoundingBox: {ex.Message}");
            }

            return intersections;
        }

        private bool IsPointInsideBoundingBox(XYZ point, BoundingBoxXYZ bbox)
        {
            return point.X >= bbox.Min.X && point.X <= bbox.Max.X &&
                   point.Y >= bbox.Min.Y && point.Y <= bbox.Max.Y &&
                   point.Z >= bbox.Min.Z && point.Z <= bbox.Max.Z;
        }

        private List<Line> CreateBoundingBoxLines(BoundingBoxXYZ bbox)
        {
            List<Line> lines = new List<Line>();

            XYZ min = bbox.Min;
            XYZ max = bbox.Max;

            // Линии в плоскости XY (на уровне колонны)
            lines.Add(Line.CreateBound(new XYZ(min.X, min.Y, min.Z), new XYZ(max.X, min.Y, min.Z)));
            lines.Add(Line.CreateBound(new XYZ(max.X, min.Y, min.Z), new XYZ(max.X, max.Y, min.Z)));
            lines.Add(Line.CreateBound(new XYZ(max.X, max.Y, min.Z), new XYZ(min.X, max.Y, min.Z)));
            lines.Add(Line.CreateBound(new XYZ(min.X, max.Y, min.Z), new XYZ(min.X, min.Y, min.Z)));

            return lines;
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

            using (Transaction trans = new Transaction(doc, "Place Intersection Annotations"))
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