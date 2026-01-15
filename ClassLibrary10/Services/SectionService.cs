
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClassLibrary10.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClassLibrary10.Abstractions.ISectionService;


namespace ClassLibrary10.Services
{
    internal class SectionService : ISectionService
    {
        private readonly ExternalCommandData _commandData;


        public SectionService(
            ExternalCommandData commandData)
        {
            _commandData = commandData;
        }

        public bool CreateSection(
            FamilyInstance instance,
            double widthOffsetMm,
            double depthOffsetMm,
            double heightOffsetMm,
            string sectionName,
            XYZ viewDirection = null, XYZ verticalDirection = null)
        {
            var doc = _commandData.Application.ActiveUIDocument.Document;

            var bbox = instance.get_BoundingBox(null);

            if (bbox == null)
            {
                return false;
            }

            var center = (bbox.Min + bbox.Max) / 2;
            var size = bbox.Max - bbox.Min;



            Transform transform = Transform.CreateTranslation(XYZ.Zero);
            transform.Origin = center;
            transform.BasisX = (viewDirection.CrossProduct(verticalDirection)).Normalize();
            transform.BasisY = viewDirection;
            transform.BasisZ = verticalDirection;

            //transform.BasisX = (XYZ.BasisZ.CrossProduct(XYZ.BasisY)).Normalize();
            //transform.BasisY = XYZ.BasisZ;
            //transform.BasisZ = XYZ.BasisY;


            var widthOffset = UnitUtils.ConvertToInternalUnits(widthOffsetMm, DisplayUnitType.DUT_MILLIMETERS);
            var depthOffset = UnitUtils.ConvertToInternalUnits(depthOffsetMm, DisplayUnitType.DUT_MILLIMETERS);
            var heightOffset = UnitUtils.ConvertToInternalUnits(heightOffsetMm, DisplayUnitType.DUT_MILLIMETERS);

            var sectionBox = new BoundingBoxXYZ();
            sectionBox.Transform = transform;
            sectionBox.Min = new XYZ(-size.X / 2 - widthOffset, -size.Z / 2 - depthOffset, -size.Y / 2 - heightOffset);
            sectionBox.Max = new XYZ(size.X / 2 + widthOffset, size.Z / 2 + depthOffset, size.Y / 2 + heightOffset);

            var viewType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .OfType<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.Section);

            if (viewType == null)
            {
                return false;
            }

            try
            {
                using (var transaction = new Transaction(doc, "VisualizeTransform"))
                {
                    transaction.Start();
                    var viewSection = ViewSection.CreateSection(doc, viewType.Id, sectionBox);
                    viewSection.Name = sectionName;

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }
    }
}
