using Autodesk.Revit.DB;
using CodeCave.Threejs.Entities;
using Material = Autodesk.Revit.DB.Material;

namespace CodeCave.Threejs.Revit.Exporter
{
    public static class MaterialExtensions
    {
        /// <summary>
        ///     Converts the material to <see cref="MeshPhongMaterial"/> instance.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <returns></returns>
        public static MeshPhongMaterial ToMeshPhong(this Material material)
        {
            if (material is null)
                throw new System.ArgumentNullException(nameof(material));

            var materialColor = material.Color.ToInt();
            var meshPhong = new MeshPhongMaterial
            {
                Uuid = material.UniqueId,
                Name = material.Name,
                Color = materialColor,
                Ambient = materialColor,
                Emissive = 0,
                Specular = materialColor,
                Shininess = 1,
                Opacity = (100D - material.Transparency) / 100,
                Transparent = material.Transparency > 0,
                Wireframe = false,
            };
            return meshPhong;
        }

        /// <summary>
        ///     Converts the material node to <see cref="MeshPhongMaterial"/> instance.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <returns></returns>
        public static MeshPhongMaterial ToMeshPhong(this MaterialNode material)
        {
            if (material is null)
                throw new System.ArgumentNullException(nameof(material));

            var materialColor = material.Color.ToInt();
            var materialUuid = $@"MaterialNode_{materialColor}_{material.Transparency * 100:0.##}";
            var meshPhong = new MeshPhongMaterial
            {
                Uuid = materialUuid,
                Color = materialColor,
                Ambient = materialColor,
                Emissive = 0,
                Specular = materialColor,
                Shininess = 1,
                Opacity = (100D - material.Transparency) / 100,
                Transparent = material.Transparency > 0,
                Wireframe = false
            };
            return meshPhong;
        }
    }
}
