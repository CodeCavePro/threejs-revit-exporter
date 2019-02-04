using System;
using Autodesk.Revit.DB;
using CodeCave.Revit.Threejs.Exporter.Materials;
using Material = Autodesk.Revit.DB.Material;

namespace CodeCave.Revit.Threejs.Exporter
{
    public static class ElementExtensions
    {
        /// <summary>
        ///     Gets the description of the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public static string GetDescription(this Element element)
        {
            if (element == null) return "<null>";

            var typeName = element.GetType().Name;
            var categoryName = element.Category?.Name ?? string.Empty;

            var fi = element as FamilyInstance;
            var symbolName = fi?.Symbol?.Name;
            var familyName = fi?.Symbol?.Family?.Name ?? string.Empty;

            var description = Equals(element.Name, typeName)
                ? $"{typeName} {categoryName} {familyName}"
                : $"{typeName} {categoryName} {familyName} {symbolName}";

            return $"{description.Replace("  ", " ")} <{element.Id?.IntegerValue ?? 0} {element.Name}>";
        }
    }

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

    public static class DoubleExtensions
    {
        /// <summary>
        ///     Converts Revit length (feet) value to millimeters.
        /// </summary>
        /// <param name="length">The length to convert.</param>
        /// <returns></returns>
        public static long LengthToMillimeters(this double length)
        {
            return 1.0e-9 > Math.Abs(length)
                ? 0
                : (long) (304.8D * length + 0.5D * Math.Sign(length));
        }
    }

    public static class MaterialExtensions
    {
        /// <summary>
        ///     Converts the material to <see cref="MeshPhongMaterial"/> instance.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <returns></returns>
        public static MeshPhongMaterial ToMeshPhong(this Material material)
        {
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
                Transparent = 0 < material.Transparency,
                Wireframe = false
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
            var materialColor = material.Color.ToInt();
            var materialUuid = $@"MaterialNode_{materialColor}_{(material.Transparency * 100):0.##}";
            var meshPhong = new MeshPhongMaterial
            {
                Uuid = materialUuid,
                Color = materialColor,
                Ambient = materialColor,
                Emissive = 0,
                Specular = materialColor,
                Shininess = 1,
                Opacity = (100D - material.Transparency) / 100,
                Transparent = 0 < material.Transparency,
                Wireframe = false
            };
            return meshPhong;
        }
    }
}