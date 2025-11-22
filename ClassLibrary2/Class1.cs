using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;


namespace ClassLibrary2
{

    [Transaction(TransactionMode.Manual)]
    public class CommandClass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Application app = uiApp.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Получаем все стены с объемами
            var walls = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .Where(w => w.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.HasValue == true)
                .ToList();

            // Находим максимальный объем и стену за один проход
            double maxLength = 0;
            Wall longestWall = null;
            double minLength = 100000;
            Wall shortestWall = null;
            double AverageLength = 0;
            int numberWalls = 0;

            foreach (Wall wall in walls)
            {
                double LengthM3 = UnitUtils.ConvertFromInternalUnits(
                    wall.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble(),
                    DisplayUnitType.DUT_CUBIC_METERS);
                numberWalls = numberWalls + 1;
                AverageLength= AverageLength+ LengthM3;

                if (LengthM3 > maxLength)
                {
                    maxLength = LengthM3;
                    longestWall = wall;
                }
                else {
                    minLength = LengthM3;
                    shortestWall = wall;
                }
            }
            AverageLength = AverageLength / numberWalls;

            // Записываем комментарий ТОЛЬКО для самой большой стены
            using (Transaction t = new Transaction(doc, "Длина в комментарий"))
            {
                t.Start();

                if (longestWall != null)
                {
                    Parameter commentParam = longestWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    commentParam.Set($"Самая длинная стена - {maxLength:F2} м");
                }
                if (shortestWall != null)
                {
                    Parameter commentParam = shortestWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    commentParam.Set($"Самая короткая стена - {minLength:F2} м");
                }

                t.Commit();

                if (longestWall != null)
                {
                    TaskDialog.Show("Готово",
                        $"Самая длинная стена: {maxLength:F2} м\n" +
                        $"ID: {longestWall.Id}\n" +
                        $"Самая короткая стена: {minLength:F2} м\n" +
                        $"ID: {shortestWall.Id}\n"+
                        $"Средняя длина стен: {AverageLength:F2} м");
                }
                else
                {
                    TaskDialog.Show("Готово", "Стены не найдены");
                }
            }

            return Result.Succeeded;
        }
    }
}


