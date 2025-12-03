using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace ClassLibrary7
{
    public class Class1
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

                List<HostObject> HostObjects = new List<HostObject>();
               

                try
                {
                    IList<Reference> pickedRefs = uiDoc.Selection.PickObjects(ObjectType.Element, new HostObjectFilter(), "Выберите элементы");

                    foreach (var pickedRef in pickedRefs)
                    {
                        Element element = doc.GetElement(pickedRef);
                        if (element is HostObject)
                        {
                            HostObjects.Add(element as HostObject);
                        }
                    }
                    TaskDialog.Show("Инфо", $"{HostObjects.Count}");
                }
                catch
                {
                    TaskDialog.Show("Инфо", $"Элементы не выбраны");
                }



                List<Solid> SolidObjects = new List<Solid>();
                double volume = 0;
                double area = 0;
                int faceCount = 0;
                int edgeCount = 0;
                double edgelenght = 0;

                Options optionsWithRefs = new Options
                {
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Medium
                };


                foreach (var Objects in HostObjects)
                {
                    var Elemenst = Objects.get_Geometry(optionsWithRefs);

                    foreach (var element in Elemenst)
                    {

                        if (element is Solid)
                        {
                            Solid solid = element as Solid;
                            //SolidObjects.Add(solid);
                            volume = volume + solid.Volume;



                            foreach (Face face in solid.Faces)
                            {
                                //Face Face = element as Face;
                                area = area + face.Area;
                                faceCount++;
                            }

                            foreach (Edge edge in solid.Edges)
                            {
                               
                                Curve curve = edge.AsCurve();

                                edgelenght = edgelenght + curve.Length;
                                edgeCount++;
                            }

                        }

                        volume=UnitUtils.ConvertFromInternalUnits( volume, DisplayUnitType.DUT_CUBIC_METERS);
                        area = UnitUtils.ConvertFromInternalUnits(area, DisplayUnitType.DUT_SQUARE_METERS);
                        edgelenght = UnitUtils.ConvertFromInternalUnits(area, DisplayUnitType.DUT_MILLIMETERS)/1000;

                    }


                }

             
                TaskDialog.Show("Инфо", $"Всего выбрано объектов - {HostObjects.Count} шт\n"
                   +$"Общий объем солидов {volume} м3\n"
                   + $"Общая площадь всех поверхностей солидов {area} м2\n"
                   + $"Общее количество граней {faceCount}\n"
                   + $"Общее количество ребер {edgeCount} и их длина {edgelenght} м");

                return Result.Succeeded;
            }
        }

        internal class HostObjectFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem is HostObject;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

    }
}

