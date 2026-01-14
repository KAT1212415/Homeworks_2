using Autodesk.Revit.DB;

namespace ClassLibrary10.Abstractions
{
   
        public interface ISectionService
        {
            bool CreateSection(FamilyInstance familyInstance, double widthOffsetMm, double depthOffsetMm, double heightOffsetMm, string sectionName, XYZ viewDirection, XYZ verticalDirection);
        }
    
}
