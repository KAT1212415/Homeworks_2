using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace ClassLibrary1
{

    [Transaction(TransactionMode.Manual)]
    public class ApplicationClass : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            application.CreateRibbonTab("ПИК-Привет");
            var panel = application.CreateRibbonPanel("ПИК-Привет", "Общее");
            var button = new PushButtonData(
                "Hello",
                "Привет",
                "C:\\Software Development Kit\\Samples\\DuplicateViews\\CS\\bin\\Debug\\DuplicateViews.dll",
                "Revit.SDK.Samples.DuplicateViews.CS.DuplicateAcrossDocumentsCommand"
                );
            //BitmapImage bitmapImage = new BitmapImage(new Uri("C:\\Users\\shvetses\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2019\\Test\\Img\\BDSBuildingPartWriter32.png", UriKind.Absolute));
            //button.LargeImage = bitmapImage;
            panel.AddItem(button);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}