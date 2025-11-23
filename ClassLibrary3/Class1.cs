using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;


namespace ClassLibrary3
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

            try
            {

                IList<Reference> pickedRefs = uiDoc.Selection.PickObjects(ObjectType.Element, new FamilyInstanceFilter(), "Выберите элементы");
                Dictionary<string, int> FamilyInstances = new Dictionary<string, int>();

                foreach (Reference reference in pickedRefs)
                {
                    
                    Element elem = doc.GetElement(reference);
                    //FamilyInstance familyInstance = elem as FamilyInstance;

                   
                        string CategoryType = ((BuiltInCategory)elem.Category.Id.IntegerValue).ToString();

                        //FamilyInstance familyInstance = elem as FamilyInstance;
                        //Category Category = familyInstance.Category;
                        //BuiltIncategory BuiltIncategory=
                       


                        bool newCategoryType = FamilyInstances.ContainsKey(CategoryType);
                        if (newCategoryType == true)
                        {
                            
                            int n = FamilyInstances[CategoryType];
                            n = n + 1;
                            FamilyInstances[CategoryType] = n;

                        }
                        else
                        {
                            FamilyInstances.Add(CategoryType, 1);
                        }
                    
                }

                string allItems = "";
                foreach (var item in FamilyInstances)
                {
                    allItems += $"{item.Key}: {item.Value}\n";
                }
                TaskDialog.Show("Все элементы", allItems);


            }
            catch
            {
                TaskDialog.Show("Инфо", $"Элементы не выбраны");
            }












            return Result.Succeeded;
        }
    }

}