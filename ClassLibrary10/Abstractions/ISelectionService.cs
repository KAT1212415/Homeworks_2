using Autodesk.Revit.DB;

namespace ClassLibrary10.Abstractions
{
    public interface ISelectionService
    {
        FamilyInstance PickFamilyInstance();
    }
}
