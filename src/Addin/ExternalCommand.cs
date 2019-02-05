#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.Exceptions;
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
        protected UIApplication uiapp;

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
            uiapp = commandData.Application;

            using (var asd = new OpenFileDialog() { Multiselect = true, CheckFileExists = true, Filter = "Revit Family Files|*.rfa" })
            {
                if (asd.ShowDialog() != DialogResult.OK)
                    return Result.Cancelled;

                var projPath = Path.ChangeExtension(Path.GetTempFileName(), ".rvt");
                var docWrapper = uiapp.Application.NewProjectDocument(UnitSystem.Metric);
                docWrapper.SaveAs(projPath, new SaveAsOptions { OverwriteExistingFile = true });
                docWrapper.Close(false);
                docWrapper = uiapp.OpenAndActivateDocument(projPath).Document;;
            
                var viewType3D = docWrapper.CreateTweakedView3D();
                uiapp.ActiveUIDocument.ActiveView = viewType3D;

                foreach (var rfaPath in asd.FileNames)
                {
                    if (!File.Exists(rfaPath))
                        continue;

                    ExportFile(docWrapper, rfaPath);
                }
            }

            return Result.Succeeded;
        }

        private void ExportFile(Document docWrapper, string rfaFilePath)
        {
            var familyName = Path.GetFileNameWithoutExtension(rfaFilePath);

            Family family;
            using (var t = new Transaction(docWrapper, $"Load the family '{familyName}'"))
            {
                t.Start();

                family = new FilteredElementCollector(docWrapper)
                    .WherePasses(new ElementClassFilter(typeof(Family), false))
                    .Cast<Family>()
                    .FirstOrDefault(f => f.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));
                
                if (family == null && !docWrapper.LoadFamily(rfaFilePath, out family))
                {
                    throw new System.InvalidOperationException();
                }

                t.Commit();
            }

            foreach (var typElementId in family.GetFamilySymbolIds())
            {
                FamilyInstance familyInstance;
                var familySymbol = docWrapper.GetElement(typElementId) as FamilySymbol;
                if (familySymbol == null)
                    throw new System.InvalidOperationException();

                var outputFilePath = familySymbol.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase)
                    ? Path.ChangeExtension(rfaFilePath, ".json")
                    : Path.ChangeExtension(rfaFilePath, $";{familySymbol.Name}.json");

                if (string.IsNullOrWhiteSpace(outputFilePath) || File.Exists(outputFilePath))
                    continue;

                using (var t = new Transaction(docWrapper, $"Placing family instance '{familySymbol.Name}'"))
                {
                    t.Start();
                    if (!familySymbol.IsActive)
                        familySymbol.Activate();

                    familyInstance = docWrapper.Create.NewFamilyInstance(
                        XYZ.Zero,
                        familySymbol,
                        StructuralType.NonStructural
                    );

                    t.Commit();
                }

                var context = new ObjectSceneExportContext(docWrapper, new FileInfo(outputFilePath));
                var exporter = new CustomExporter(docWrapper, context)
                {
                    ShouldStopOnError = false
                };
                exporter.Export(uiapp.ActiveUIDocument.ActiveView as View3D);

                using (var t = new Transaction(docWrapper, $"Remove family symbol '{familyInstance.Name}'"))
                {
                    t.Start();
                    docWrapper.Delete(familySymbol.Id);
                    t.Commit();
                }
            }

            using (var t = new Transaction(docWrapper, $"Remove family '{family.Name}'"))
            {
                t.Start();
                docWrapper.Delete(family.Id);
                t.Commit();
            }
        }
    }
}
