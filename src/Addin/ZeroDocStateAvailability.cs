using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CodeCave.Revit.Threejs.Exporter.Addin
{
    public class ZeroDocStateAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }
    }
}
