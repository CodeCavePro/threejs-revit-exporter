using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CodeCave.Threejs.Revit.Exporter.Addin
{
    public class ZeroDocStateAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }
    }
}
