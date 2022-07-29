using Autodesk.Revit.DB;
using CodeCave.Threejs.Entities;

namespace CodeCave.Threejs.Revit.Exporter
{
    public static class XYZExtensions
    {
        public static Vector3 ToVector3(this XYZ pointXYZ)
        {
            if (pointXYZ is null)
                return null;

            // Flipping X, Y, Z coordinates to match the conventional 3D coordinate system
            // https://knowledge.autodesk.com/support/recap/troubleshooting/caas/sfdcarticles/sfdcarticles/Switch-or-flip-X-Y-or-Z-axis-orientation-during-data-import.html
            return new Vector3(
                -pointXYZ.X.RevitLengthToMillimeters(),
                pointXYZ.Z.RevitLengthToMillimeters(),
                pointXYZ.Y.RevitLengthToMillimeters());
        }
    }
}
