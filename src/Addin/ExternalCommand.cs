#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

#endregion

namespace CodeCave.Revit.Threejs.Exporter
{
    /// <summary>
    /// A simple example of an external command, usually it's used for batch processing 
    /// files loaded when Revit is idling
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExternalCommand : IExternalCommand
    {
        /// <summary>
        /// Executes the specified Revit command <see cref="ExternalCommand"/>.
        /// The main Execute method (inherited from IExternalCommand) must be public.
        /// </summary>
        /// <param name="commandData">The command data / context.</param>
        /// <param name="message">The message.</param>
        /// <param name="elements">The elements.</param>
        /// <returns>The result of command execution.</returns>
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            var uiapp = commandData?.Application;
            var uidoc = uiapp?.ActiveUIDocument;
            var app = uiapp?.Application;

            ExportFile(uiapp, @"D:\Downloads\Neptune_Waste_Management_System_-_Silver_Rover_with_Docking_Station_12083.rfa");
            return Result.Succeeded;

            using (var asd = new OpenFileDialog())
            {
                if (asd.ShowDialog() != DialogResult.OK)
                    return Result.Cancelled;

                foreach (var rfaPath in asd.FileNames)
                {
                    if (!File.Exists(rfaPath))
                        continue;

                    ExportFile(uiapp, rfaPath);
                }
            }

            return Result.Succeeded;
        }

        private void ExportFile(UIApplication uiapp, string rfaFilePath)
        {
            var rfaFileName = Path.GetFileName(rfaFilePath);
            var projPath = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(rfaFileName, ".rvt"));

            Document projTmp;
            if (!(Debugger.IsAttached && File.Exists(projPath)))
            {
                projTmp = uiapp.Application.NewProjectDocument(UnitSystem.Metric);
                using (var t = new Transaction(projTmp, "Load the family"))
                {
                    t.Start();
                    FamilySymbol familySymbol = null;
                    foreach (var type in new[] { Path.GetFileNameWithoutExtension(rfaFileName), " " })
                    {
                        var familyLoaded = projTmp.LoadFamilySymbol(rfaFilePath, type, out familySymbol);
                        if (familyLoaded && familySymbol != null)
                            break;
                    }

                    if (!familySymbol.IsActive)
                        familySymbol.Activate();

                    projTmp.Create.NewFamilyInstance(XYZ.Zero, familySymbol, StructuralType.NonStructural);
                    t.Commit();
                }

                projTmp.SaveAs(projPath, new SaveAsOptions { OverwriteExistingFile = true });
                projTmp.Close(false);
            }

            View3D viewType3D;
            projTmp = uiapp.OpenAndActivateDocument(projPath).Document;
            using (var t = new Transaction(projTmp, "Change to 3D view"))
            {
                t.Start();
                var viewTypeId = FindViewTypes(projTmp, ViewType.ThreeD).First().Id;
                viewType3D = View3D.CreateIsometric(projTmp, viewTypeId);
                viewType3D.get_Parameter( BuiltInParameter.VIEW_DETAIL_LEVEL ).Set( 3 );
                viewType3D.get_Parameter( BuiltInParameter.MODEL_GRAPHICS_STYLE ).Set( 6 );
                t.Commit();
            }

            uiapp.ActiveUIDocument.ActiveView = viewType3D;

            var outputFile = new FileInfo(Path.Combine(Path.GetTempPath(), Path.ChangeExtension(rfaFileName, ".json")));
            var context = new ObjectSceneExportContext(projTmp, outputFile);
            new CustomExporter(projTmp, context) { ShouldStopOnError = false }.Export(viewType3D);
        }

         public static IEnumerable<ViewFamilyType> FindViewTypes(Document doc, ViewType viewType)
        {
            var ret = new FilteredElementCollector(doc)
                            .WherePasses(new ElementClassFilter(typeof(ViewFamilyType), false))
                            .Cast<ViewFamilyType>();

            switch (viewType)
            {
                case ViewType.AreaPlan:
                    return ret.Where(e => e.ViewFamily == ViewFamily.AreaPlan);
                case ViewType.CeilingPlan:
                    return ret.Where(e => e.ViewFamily == ViewFamily.CeilingPlan);
                case ViewType.ColumnSchedule:
                    return ret.Where(e => e.ViewFamily == ViewFamily.GraphicalColumnSchedule); //?
                case ViewType.CostReport:
                    return ret.Where(e => e.ViewFamily == ViewFamily.CostReport);
                case ViewType.Detail:
                    return ret.Where(e => e.ViewFamily == ViewFamily.Detail);
                case ViewType.DraftingView:
                    return ret.Where(e => e.ViewFamily == ViewFamily.Drafting);
                case ViewType.DrawingSheet:
                    return ret.Where(e => e.ViewFamily == ViewFamily.Sheet);
                case ViewType.Elevation:
                    return ret.Where(e => e.ViewFamily == ViewFamily.Elevation);
                case ViewType.EngineeringPlan:
                    return ret.Where(e => e.ViewFamily == ViewFamily.StructuralPlan); //?
                case ViewType.FloorPlan:
                    return ret.Where(e => e.ViewFamily == ViewFamily.FloorPlan);
                //case ViewType.Internal:
                //    return ret.Where(e => e.ViewFamily == ViewFamily.Internal); //???
                case ViewType.Legend:
                    return ret.Where(e => e.ViewFamily == ViewFamily.Legend);
                case ViewType.LoadsReport:
                    return ret.Where(e => e.ViewFamily == ViewFamily.LoadsReport);
                case ViewType.PanelSchedule:
                    return ret.Where(e => e.ViewFamily == ViewFamily.PanelSchedule);
                case ViewType.PresureLossReport:
                    return ret.Where(e => e.ViewFamily == ViewFamily.PressureLossReport);
                case ViewType.Rendering:
                    return ret.Where(e => e.ViewFamily == ViewFamily.ImageView); //?
                //case ViewType.Report:
                //    return ret.Where(e => e.ViewFamily == ViewFamily.Report); //???
                case ViewType.Schedule:
                    return ret.Where(e => e.ViewFamily == ViewFamily.Schedule);
                case ViewType.Section:
                    return ret.Where(e => e.ViewFamily == ViewFamily.Section);
                case ViewType.ThreeD:
                    return ret.Where(e => e.ViewFamily == ViewFamily.ThreeDimensional);
                case ViewType.Undefined:
                    return ret.Where(e => e.ViewFamily == ViewFamily.Invalid);  //?
                case ViewType.Walkthrough:
                    return ret.Where(e => e.ViewFamily == ViewFamily.Walkthrough);
                default:
                    return ret;
            }
        }
    }
}
