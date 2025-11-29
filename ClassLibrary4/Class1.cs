using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace ClassLibrary4
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

            //Получение списка стен

            List<Element> selectedElements = new List<Element>();
           

            //Выбор только 2х стен

            while (selectedElements.Count < 2)
            {
                try
                {
                    Reference reference = uiDoc.Selection.PickObject(
                        ObjectType.Element,
                        $"Выберите элемент "
                    );

                    Element elem = doc.GetElement(reference);
                    if (elem != null && !selectedElements.Contains(elem))
                    {
                        selectedElements.Add(elem);
                    }
                }
                catch
                {
                    TaskDialog.Show("Инфо", $"Элементы не выбраны");
                }
            }

            //Получение векторов стен
            XYZ direction1 = GetWallNormal(selectedElements[0]);
            XYZ direction2 = GetWallNormal(selectedElements[1]);

            //Скалярное произведение векторов
            double dotProduct = direction1.DotProduct(direction2);
            double dotProduct2 = Math.Abs(dotProduct);

            //Если векторы параллельны,нахождение вектора между точками центров стен
            if (dotProduct2 == 1)
            {
                
                
                TaskDialog.Show("Инфо", $"Стены параллельны");
                XYZ point1 = GetWallMidpoint(selectedElements[0]);
                XYZ point2 = GetWallMidpoint(selectedElements[1]);
                XYZ vector1 = point1 - point2;
                
                //Получение проекции вектора, соединяющего центры стен на нормаль стены
                var result = vector1.DotProduct(direction1);
                //Получение конца вектора
                XYZ point3 = point1 - direction1 * result;
                //Визуализация векторов можно не использовать, для наглядности
                using (var transaction = new Transaction(doc, "DotProduct vectors"))
                {
                    transaction.Start();
                    VisualizeAsVector(doc, point1, point3);                 
                    transaction.Commit();
                }
                // Вывод длины вектора в окне
                double areaM2 = UnitUtils.ConvertFromInternalUnits(result,DisplayUnitType.DUT_MILLIMETERS);
                TaskDialog.Show("Итог", $"{areaM2.ToString()} мм  ");

            }
            else { TaskDialog.Show("Инфо", $"Стены не параллельны"); }

            return Result.Succeeded;
        }
    
        //Нахождение нормали стены
    public XYZ GetWallNormal(Element wall)
        {
            Wall wall1= wall as Wall;

            LocationCurve location =
                             wall1.Location as LocationCurve;
            Curve curve = location.Curve;

            XYZ wallDirection =
              (curve.GetEndPoint(1) - curve.GetEndPoint(0))
              .Normalize();
            XYZ up = XYZ.BasisZ;

            // Нормаль к стене 
            // (перпендикулярно направлению и вертикали)
            return wallDirection
              .CrossProduct(up)
              .Normalize();
        }
        //Нахождение средней точки стены
        public XYZ GetWallMidpoint(Element wall)

        {   

            Wall midpoint = wall as Wall;
            if (wall?.Location is LocationCurve locationCurve)
            {
                Curve curve = locationCurve.Curve;
                XYZ start = curve.GetEndPoint(0);
                XYZ end = curve.GetEndPoint(1);

                // Середина = среднее арифметическое точек
                return (start + end) / 2.0;
            }
            return null;
        }

        //Визуализация вектора в модели
        public void VisualizeAsVector(Document doc, XYZ endPoint, XYZ startPoint )
        {
            
            var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            directShape.SetShape(new List<GeometryObject>() { Line.CreateBound(startPoint, endPoint) });
        }

    }
}













