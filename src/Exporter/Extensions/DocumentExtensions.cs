using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace CodeCave.Threejs.Revit.Exporter
{
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