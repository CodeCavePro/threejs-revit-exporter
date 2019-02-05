using System;
using System.Collections.Generic;
using System.Linq;
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

    public static class DocumentExtensions
    {
        public static IEnumerable<ViewFamilyType> FindViewTypes(this Document document, ViewType viewType)
        {
            var result = new FilteredElementCollector(document)
                            .WherePasses(new ElementClassFilter(typeof(ViewFamilyType), false))
                            .Cast<ViewFamilyType>();

            switch (viewType)
            {
                case ViewType.AreaPlan:
                    return result.Where(e => e.ViewFamily == ViewFamily.AreaPlan);
                case ViewType.CeilingPlan:
                    return result.Where(e => e.ViewFamily == ViewFamily.CeilingPlan);
                case ViewType.ColumnSchedule:
                    return result.Where(e => e.ViewFamily == ViewFamily.GraphicalColumnSchedule); //?
                case ViewType.CostReport:
                    return result.Where(e => e.ViewFamily == ViewFamily.CostReport);
                case ViewType.Detail:
                    return result.Where(e => e.ViewFamily == ViewFamily.Detail);
                case ViewType.DraftingView:
                    return result.Where(e => e.ViewFamily == ViewFamily.Drafting);
                case ViewType.DrawingSheet:
                    return result.Where(e => e.ViewFamily == ViewFamily.Sheet);
                case ViewType.Elevation:
                    return result.Where(e => e.ViewFamily == ViewFamily.Elevation);
                case ViewType.EngineeringPlan:
                    return result.Where(e => e.ViewFamily == ViewFamily.StructuralPlan); //?
                case ViewType.FloorPlan:
                    return result.Where(e => e.ViewFamily == ViewFamily.FloorPlan);
                //case ViewType.Internal:
                //    return ret.Where(e => e.ViewFamily == ViewFamily.Internal); //???
                case ViewType.Legend:
                    return result.Where(e => e.ViewFamily == ViewFamily.Legend);
                case ViewType.LoadsReport:
                    return result.Where(e => e.ViewFamily == ViewFamily.LoadsReport);
                case ViewType.PanelSchedule:
                    return result.Where(e => e.ViewFamily == ViewFamily.PanelSchedule);
                case ViewType.PresureLossReport:
                    return result.Where(e => e.ViewFamily == ViewFamily.PressureLossReport);
                case ViewType.Rendering:
                    return result.Where(e => e.ViewFamily == ViewFamily.ImageView); //?
                //case ViewType.Report:
                //    return ret.Where(e => e.ViewFamily == ViewFamily.Report); //???
                case ViewType.Schedule:
                    return result.Where(e => e.ViewFamily == ViewFamily.Schedule);
                case ViewType.Section:
                    return result.Where(e => e.ViewFamily == ViewFamily.Section);
                case ViewType.ThreeD:
                    return result.Where(e => e.ViewFamily == ViewFamily.ThreeDimensional);
                case ViewType.Undefined:
                    return result.Where(e => e.ViewFamily == ViewFamily.Invalid);  //?
                case ViewType.Walkthrough:
                    return result.Where(e => e.ViewFamily == ViewFamily.Walkthrough);
                default:
                    return result;
            }
        }

        public static View3D CreateTweakedView3D(this Document document)
        {
            View3D view3D;

            using (var transaction = new Transaction(document, "Switch to 3D view and tweak it"))
            {
                transaction.Start();

                var viewTypeId = FindViewTypes(document, ViewType.ThreeD).FirstOrDefault()?.Id;
                view3D = View3D.CreateIsometric(document, viewTypeId);
                view3D.get_Parameter( BuiltInParameter.VIEW_DETAIL_LEVEL ).Set( 3 );
                view3D.get_Parameter( BuiltInParameter.MODEL_GRAPHICS_STYLE ).Set( 6 );

                transaction.Commit();
            }

            return view3D;
        }
    }
}