namespace CodeCave.Revit.Threejs.Exporter.Geometries
{
    /// <summary>
    /// An efficient representation of mesh, line, or point geometry.
    /// Includes vertex positions, face indices, normals, colors, UVs,
    /// and custom attributes within buffers, reducing the cost of passing all this data to the GPU.
    /// To read and edit data in BufferGeometry attributes, see BufferAttribute documentation.
    ///
    /// For a less efficient but easier-to-use representation of geometry, see <see cref="Geometry"/>. 
    /// </summary>
    public class BufferGeometry
    {
        // TODO implement buffer geometry class and use it instead of Geometry
    }
}
