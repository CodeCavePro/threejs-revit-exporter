using System;
using System.Diagnostics.CodeAnalysis;
using Autodesk.Revit.DB;

namespace CodeCave.Threejs.Revit.Exporter.Helpers
{
    public class Point3D : IEquatable<Point3D>, IComparable<Point3D>
    {
        public readonly long X, Y, Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point3D"/> class.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        public Point3D(long x, long y, long z)
        {
            X = x;
            Y = y;
            Z = z;
        }


        /// <summary>
        /// Performs an implicit conversion from <see cref="Autodesk.Revit.DB.XYZ"/> to <see cref="Point3D"/>.
        /// </summary>
        /// <param name="pointXYZ">The XYZ point.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "We don't need one.")]
        public static implicit operator Point3D(XYZ pointXYZ)
        {
            if (pointXYZ is null)
                return null;

            // Flipping X, Y, Z coordinates to match the conventional 3D coordinate system
            // https://knowledge.autodesk.com/support/recap/troubleshooting/caas/sfdcarticles/sfdcarticles/Switch-or-flip-X-Y-or-Z-axis-orientation-during-data-import.html
            return new Point3D(
                -pointXYZ.X.RevitLengthToMillimeters(),
                pointXYZ.Z.RevitLengthToMillimeters(),
                pointXYZ.Y.RevitLengthToMillimeters()
            );
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Point3D other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == GetType() && Equals((Point3D) obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return $@"{X},{Y},{Z}".GetHashCode();
        }

        /// <summary>
        /// Compares to.
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns></returns>
        public int CompareTo(Point3D a)
        {
            long deltaAxis;
            if ((deltaAxis = X - a.X) == 0)
            {
                deltaAxis = (deltaAxis = Y - a.Y) == 0
                    ? Z - a.Z
                    : deltaAxis;
            }

            return (deltaAxis == 0)
                ? 0
                : deltaAxis > 0
                    ? 1
                    : -1;
        }
    }
}
