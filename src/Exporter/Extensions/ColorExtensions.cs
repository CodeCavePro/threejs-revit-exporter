using Autodesk.Revit.DB;

namespace CodeCave.Threejs.Revit.Exporter
{
    public static class ColorExtensions
    {
        /// <summary>
        ///     Converts Revit <see cref="Color" /> to an integer.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        public static int ToInt(this Color color)
        {
            return (color.Red << 16)
                   | (color.Green << 8)
                   | color.Blue;
        }
    }
}