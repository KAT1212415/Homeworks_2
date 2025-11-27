using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace SimpleCirclePlugin
{
    [Transaction(TransactionMode.Manual)]
    public class CreateSimpleFloor : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // 1. Выбор уровня
                Level level = SelectLevel(doc);
                if (level == null) return Result.Cancelled;

                // 2. Выбор нескольких помещений
                List<Room> rooms = SelectMultipleRooms(uiDoc);
                if (rooms == null || rooms.Count == 0) return Result.Cancelled;

                // 3. Выбор типа перекрытия
                FloorType floorType = SelectFloorType(doc);
                if (floorType == null) return Result.Cancelled;

                // 4. Создание перекрытий для всех выбранных помещений
                int floorsCreated = CreateFloorsForMultipleRooms(doc, rooms, level, floorType);

                if (floorsCreated > 0)
                {
                    TaskDialog.Show("Успех",
                        $"✅ Создано {floorsCreated} перекрытий!\n" +
                        $"📐 Уровень: {level.Name}\n" +
                        $"🏗️ Тип: {floorType.Name}\n" +
                        $"🏠 Помещений: {rooms.Count}");
                }
                else
                {
                    TaskDialog.Show("Ошибка", "Не удалось создать перекрытия");
                    return Result.Failed;
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
                return Result.Failed;
            }
        }

        private Level SelectLevel(Document doc)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (levels.Count == 0)
            {
                TaskDialog.Show("Ошибка", "Уровни не найдены");
                return null;
            }

            return levels[0];
        }

        private List<Room> SelectMultipleRooms(UIDocument uiDoc)
        {
            try
            {
                TaskDialog.Show("Выбор помещений",
                    "Выберите несколько помещений для создания перекрытий:\n\n" +
                    "• Удерживайте Ctrl для выбора нескольких помещений\n" +
                    "• Или выделите прямоугольником несколько помещений\n" +
                    "• Нажмите Enter для завершения выбора");

                IList<Reference> roomRefs = uiDoc.Selection.PickObjects(
                    ObjectType.Element,
                    new RoomFilter(),
                    "Выберите помещения для создания перекрытий");

                if (roomRefs == null || roomRefs.Count == 0)
                {
                    TaskDialog.Show("Отмена", "Помещения не выбраны");
                    return null;
                }

                List<Room> rooms = new List<Room>();
                foreach (Reference roomRef in roomRefs)
                {
                    Room room = uiDoc.Document.GetElement(roomRef) as Room;
                    if (room != null)
                    {
                        rooms.Add(room);
                    }
                }

                if (rooms.Count == 0)
                {
                    TaskDialog.Show("Ошибка", "Не удалось получить выбранные помещения");
                    return null;
                }

                string roomNames = string.Join("\n", rooms.Take(3).Select(r => $"• {GetRoomName(r)}"));
                if (rooms.Count > 3)
                {
                    roomNames += $"\n• ... и еще {rooms.Count - 3} помещений";
                }

                TaskDialog.Show("Выбор завершен",
                    $"✅ Выбрано {rooms.Count} помещений:\n{roomNames}");

                return rooms;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Ошибка выбора помещений: {ex.Message}");
                return null;
            }
        }

        private FloorType SelectFloorType(Document doc)
        {
            var floorTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .OrderBy(ft => ft.Name)
                .ToList();

            if (floorTypes.Count == 0)
            {
                TaskDialog.Show("Ошибка", "Типы перекрытий не найдены");
                return null;
            }

            return ShowAllFloorTypesDialog(floorTypes);
        }

        private FloorType ShowAllFloorTypesDialog(List<FloorType> floorTypes)
        {
            try
            {
                if (floorTypes.Count <= 4)
                {
                    return ShowFloorTypeSelectionPage(floorTypes, 0, "Выберите тип перекрытия:");
                }

                int currentPage = 0;
                int pageSize = 4;
                int totalPages = (int)Math.Ceiling((double)floorTypes.Count / pageSize);

                while (true)
                {
                    int startIndex = currentPage * pageSize;
                    int endIndex = Math.Min(startIndex + pageSize, floorTypes.Count);
                    var pageTypes = floorTypes.GetRange(startIndex, endIndex - startIndex);

                    string instruction = $"Выберите тип перекрытия (страница {currentPage + 1} из {totalPages}):";

                    FloorType selectedType = ShowFloorTypeSelectionPage(pageTypes, startIndex, instruction);

                    if (selectedType != null) return selectedType;

                    currentPage++;
                    if (currentPage >= totalPages) currentPage = 0;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", "Ошибка выбора типа перекрытия: " + ex.Message);
                return floorTypes[0];
            }
        }

        private FloorType ShowFloorTypeSelectionPage(List<FloorType> pageTypes, int startIndex, string instruction)
        {
            var dialog = new TaskDialog("Выбор типа перекрытия");
            dialog.MainInstruction = instruction;
            dialog.CommonButtons = TaskDialogCommonButtons.Cancel;

            if (pageTypes.Count == 4)
            {
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink4, "➡️ Следующая страница...");
            }

            for (int i = 0; i < pageTypes.Count && i < 3; i++)
            {
                var floorType = pageTypes[i];
                string thicknessInfo = GetFloorThicknessInfo(floorType);
                dialog.AddCommandLink(
                    TaskDialogCommandLinkId.CommandLink1 + i,
                    $"{floorType.Name} {thicknessInfo}");
            }

            TaskDialogResult result = dialog.Show();

            if (result == TaskDialogResult.Cancel) return null;

            int resultIndex = (int)result - (int)TaskDialogCommandLinkId.CommandLink1;

            if (resultIndex == 3 && pageTypes.Count == 4) return null;

            if (resultIndex >= 0 && resultIndex < pageTypes.Count) return pageTypes[resultIndex];

            return null;
        }

        private string GetFloorThicknessInfo(FloorType floorType)
        {
            try
            {
                double thickness = GetFloorThickness(floorType);
                return $"(Толщина: {Math.Round(thickness * 304.8)} мм)";
            }
            catch
            {
                return "(Толщина: неизвестна)";
            }
        }

        // ★★★ ИСПРАВЛЕННЫЙ МЕТОД: РАЗДЕЛЕНИЕ ТРАНЗАКЦИЙ ★★★
        private int CreateFloorsForMultipleRooms(Document doc, List<Room> rooms, Level level, FloorType floorType)
        {
            int floorsCreated = 0;
            List<ElementId> createdFloorIds = new List<ElementId>();
            Dictionary<ElementId, List<Element>> floorsToCut = new Dictionary<ElementId, List<Element>>();

            // ТРАНЗАКЦИЯ 1: СОЗДАНИЕ ПЕРЕКРЫТИЙ
            using (Transaction trans1 = new Transaction(doc, "Create Floors"))
            {
                trans1.Start();

                try
                {
                    foreach (Room room in rooms)
                    {
                        Floor floor = CreateBaseFloor(doc, room, level, floorType);
                        if (floor != null)
                        {
                            floorsCreated++;
                            createdFloorIds.Add(floor.Id);

                            // Сохраняем информацию о том, какие элементы нужно вырезать для этого перекрытия
                            List<Element> elementsToCut = FindElementsToCut(doc, room, level);
                            floorsToCut.Add(floor.Id, elementsToCut);
                        }
                    }

                    trans1.Commit();
                }
                catch (Exception ex)
                {
                    trans1.RollBack();
                    throw new Exception($"Ошибка создания перекрытий: {ex.Message}");
                }
            }

            // ТРАНЗАКЦИЯ 2: СОЗДАНИЕ ВЫРЕЗОВ
            int openingsCreated = 0;
            if (createdFloorIds.Count > 0)
            {
                using (Transaction trans2 = new Transaction(doc, "Create Floor Openings"))
                {
                    trans2.Start();

                    try
                    {
                        foreach (ElementId floorId in createdFloorIds)
                        {
                            Floor floor = doc.GetElement(floorId) as Floor;
                            if (floor != null && floorsToCut.ContainsKey(floorId))
                            {
                                List<Element> elementsToCut = floorsToCut[floorId];
                                openingsCreated += CreateOpeningsForFloor(doc, floor, elementsToCut, level);
                            }
                        }

                        trans2.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans2.RollBack();
                        // Не прерываем выполнение, просто логируем ошибку
                        System.Diagnostics.Debug.WriteLine($"Ошибка создания вырезов: {ex.Message}");
                    }
                }
            }

            // Показываем статистику
            if (floorsCreated > 0)
            {
                string message = $"✅ Успешно создано:\n" +
                                $"🏗️ Перекрытий: {floorsCreated}\n" +
                                $"🔲 Вырезов: {openingsCreated}\n" +
                                $"📐 Уровень: {level.Name}";

                if (floorsCreated < rooms.Count)
                {
                    message += $"\n\n⚠️ Не удалось создать {rooms.Count - floorsCreated} перекрытий";
                }

                TaskDialog.Show("Результат", message);
            }

            return floorsCreated;
        }

        private Floor CreateBaseFloor(Document doc, Room room, Level level, FloorType floorType)
        {
            try
            {
                var boundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
                if (boundaries == null || boundaries.Count == 0) return null;

                CurveArray curveArray = new CurveArray();
                var firstLoop = boundaries[0];

                foreach (BoundarySegment segment in firstLoop)
                {
                    Curve curve = segment.GetCurve();
                    if (curve != null) curveArray.Append(curve);
                }

                Floor floor = doc.Create.NewFloor(curveArray, floorType, level, false, XYZ.BasisZ);

                if (floor != null)
                {
                    double floorThickness = GetFloorThickness(floorType);
                    Parameter offsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                    if (offsetParam != null && !offsetParam.IsReadOnly)
                    {
                        offsetParam.Set(floorThickness);
                    }

                    string floorName = $"{GetRoomName(room)}_Перекрытие";
                    try { floor.Name = floorName; } catch { }
                }

                return floor;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания перекрытия для помещения {GetRoomName(room)}: {ex.Message}");
                return null;
            }
        }

        // ★★★ ИЗМЕНЕННЫЙ МЕТОД: ПРИНИМАЕТ ГОТОВЫЙ СПИСОК ЭЛЕМЕНТОВ ★★★
        private int CreateOpeningsForFloor(Document doc, Floor floor, List<Element> elementsToCut, Level level)
        {
            int openingsCreated = 0;

            try
            {
                foreach (Element element in elementsToCut)
                {
                    if (CreateOpeningForElement(doc, floor, element, level))
                    {
                        openingsCreated++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания вырезов для перекрытия {floor.Id}: {ex.Message}");
            }

            return openingsCreated;
        }

        private List<Element> FindElementsToCut(Document doc, Room room, Level level)
        {
            List<Element> elements = new List<Element>();

            try
            {
                BoundingBoxXYZ roomBBox = room.get_BoundingBox(null);
                if (roomBBox == null) return elements;

                // СТЕНЫ
                var walls = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Walls)
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .Where(w => IsElementOnLevel(w, level))
                    .ToList();

                // КОЛОННЫ
                var archColumns = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Columns)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .Where(c => IsElementOnLevel(c, level))
                    .ToList();

                var structColumns = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .Where(c => IsElementOnLevel(c, level))
                    .ToList();

                // ПРОВЕРКА ПЕРЕСЕЧЕНИЯ
                foreach (Wall wall in walls)
                {
                    if (DoesElementIntersectBBox(wall, roomBBox))
                    {
                        elements.Add(wall);
                    }
                }

                foreach (var column in archColumns)
                {
                    if (DoesElementIntersectBBox(column, roomBBox))
                    {
                        elements.Add(column);
                    }
                }

                foreach (var column in structColumns)
                {
                    if (DoesElementIntersectBBox(column, roomBBox))
                    {
                        elements.Add(column);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка поиска элементов: " + ex.Message);
            }

            return elements;
        }

        private bool DoesElementIntersectBBox(Element element, BoundingBoxXYZ roomBBox)
        {
            try
            {
                BoundingBoxXYZ elemBBox = element.get_BoundingBox(null);
                if (elemBBox == null) return false;

                return elemBBox.Min.X <= roomBBox.Max.X && elemBBox.Max.X >= roomBBox.Min.X &&
                       elemBBox.Min.Y <= roomBBox.Max.Y && elemBBox.Max.Y >= roomBBox.Min.Y;
            }
            catch
            {
                return false;
            }
        }

        private bool CreateOpeningForElement(Document doc, Floor floor, Element element, Level level)
        {
            try
            {
                CurveArray openingCurves = null;

                if (element is Wall wall)
                {
                    openingCurves = CreateWallOpeningCurves(wall, level);
                }
                else if (element is FamilyInstance column)
                {
                    openingCurves = CreateColumnOpeningCurves(column, level);
                }

                if (openingCurves != null && openingCurves.Size > 0)
                {
                    doc.Create.NewOpening(floor, openingCurves, true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания выреза для {element.Id}: {ex.Message}");
            }

            return false;
        }

        private CurveArray CreateWallOpeningCurves(Wall wall, Level level)
        {
            try
            {
                LocationCurve wallLocation = wall.Location as LocationCurve;
                if (wallLocation == null) return null;

                Curve wallCurve = wallLocation.Curve;
                if (!(wallCurve is Line)) return null;

                Line wallLine = wallCurve as Line;

                double wallThickness = GetWallThickness(wall);
                double offset = wallThickness * 1.5;

                XYZ direction = wallLine.Direction;
                XYZ perpendicular = new XYZ(-direction.Y, direction.X, 0).Normalize();

                XYZ start = wallLine.GetEndPoint(0);
                XYZ end = wallLine.GetEndPoint(1);

                XYZ p1 = start + perpendicular * offset;
                XYZ p2 = start - perpendicular * offset;
                XYZ p3 = end - perpendicular * offset;
                XYZ p4 = end + perpendicular * offset;

                CurveArray curves = new CurveArray();
                curves.Append(Line.CreateBound(p1, p2));
                curves.Append(Line.CreateBound(p2, p3));
                curves.Append(Line.CreateBound(p3, p4));
                curves.Append(Line.CreateBound(p4, p1));

                return curves;
            }
            catch
            {
                return null;
            }
        }

        private CurveArray CreateColumnOpeningCurves(FamilyInstance column, Level level)
        {
            try
            {
                BoundingBoxXYZ columnBBox = column.get_BoundingBox(null);
                if (columnBBox == null) return null;

                double width = (columnBBox.Max.X - columnBBox.Min.X) * 1.5;
                double depth = (columnBBox.Max.Y - columnBBox.Min.Y) * 1.5;
                double centerX = (columnBBox.Min.X + columnBBox.Max.X) / 2;
                double centerY = (columnBBox.Min.Y + columnBBox.Max.Y) / 2;

                double halfWidth = width / 2;
                double halfDepth = depth / 2;

                XYZ p1 = new XYZ(centerX - halfWidth, centerY - halfDepth, level.Elevation);
                XYZ p2 = new XYZ(centerX + halfWidth, centerY - halfDepth, level.Elevation);
                XYZ p3 = new XYZ(centerX + halfWidth, centerY + halfDepth, level.Elevation);
                XYZ p4 = new XYZ(centerX - halfWidth, centerY + halfDepth, level.Elevation);

                CurveArray curves = new CurveArray();
                curves.Append(Line.CreateBound(p1, p2));
                curves.Append(Line.CreateBound(p2, p3));
                curves.Append(Line.CreateBound(p3, p4));
                curves.Append(Line.CreateBound(p4, p1));

                return curves;
            }
            catch
            {
                return null;
            }
        }

        private bool IsElementOnLevel(Element element, Level level)
        {
            try
            {
                Parameter levelParam = element.get_Parameter(BuiltInParameter.LEVEL_PARAM);
                if (levelParam != null && levelParam.AsElementId() == level.Id)
                    return true;

                Parameter baseLevelParam = element.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
                if (baseLevelParam != null && baseLevelParam.AsElementId() == level.Id)
                    return true;

                Parameter topLevelParam = element.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                if (topLevelParam != null && topLevelParam.AsElementId() == level.Id)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private double GetWallThickness(Wall wall)
        {
            try
            {
                Parameter thicknessParam = wall.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);
                if (thicknessParam != null && thicknessParam.HasValue)
                {
                    return thicknessParam.AsDouble();
                }
            }
            catch { }

            return 0.2;
        }

        private double GetFloorThickness(FloorType floorType)
        {
            try
            {
                Parameter thicknessParam = floorType.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
                if (thicknessParam != null && thicknessParam.HasValue)
                {
                    return thicknessParam.AsDouble();
                }

                CompoundStructure structure = floorType.GetCompoundStructure();
                if (structure != null)
                {
                    double totalThickness = 0.0;
                    for (int i = 0; i < structure.LayerCount; i++)
                    {
                        totalThickness += structure.GetLayerWidth(i);
                    }
                    return totalThickness;
                }
            }
            catch { }

            return 0.2;
        }

        private string GetRoomName(Room room)
        {
            if (room == null) return "Неизвестно";
            string number = room.Number;
            string name = room.Name;
            if (!string.IsNullOrEmpty(number) && !string.IsNullOrEmpty(name))
                return $"{number}_{name}";
            else if (!string.IsNullOrEmpty(number))
                return number;
            else if (!string.IsNullOrEmpty(name))
                return name;
            else
                return "Помещение";
        }

        private class RoomFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem is Room;
            public bool AllowReference(Reference reference, XYZ position) => false;
        }
    }
}