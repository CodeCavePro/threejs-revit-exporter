using System;

namespace CodeCave.Threejs.Revit.Exporter
{
    public static class DoubleExtensions
    {
        /// <summary>
        ///     Converts Revit length (feet) value to millimeters.
        /// </summary>
        /// <param name="length">The length to convert.</param>
        /// <returns></returns>
        public static long RevitLengthToMillimeters(this double length)
        {
            return Math.Abs(length) < 1.0e-9
                ? 0
                : (long)((304.8D * length) + (0.5D * Math.Sign(length)));
        }
    }
}
