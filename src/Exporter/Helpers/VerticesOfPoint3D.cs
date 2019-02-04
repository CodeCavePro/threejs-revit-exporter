using System.Collections;
using System.Collections.Generic;

namespace CodeCave.Revit.Threejs.Exporter.Helpers
{
    public class VerticesOfPoint3D : IEnumerable<Point3D>
    {
        protected IDictionary<Point3D, int> vertices = new Dictionary<Point3D, int>();

        /// <summary>
        ///     Return the index of the given vertex,
        ///     adding a new entry if required.
        /// </summary>
        public int AddVertex(Point3D p)
        {
            return vertices.ContainsKey(p)
                ? vertices[p]
                : vertices[p] = vertices.Count;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Point3D> GetEnumerator()
        {
            return vertices.Keys.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}