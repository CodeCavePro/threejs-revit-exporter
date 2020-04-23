using System.IO;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CodeCave.Threejs.Revit.Exporter.Addin
{
    /// <summary>
    /// A sample ribbon command, demonstrates the possibility to bing Revit commands to ribbon buttons.
    /// </summary>
    /// <seealso cref="IExternalCommand" />
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExporterCommand : IExternalCommand
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
            ElementSet elements)
        {
            if (commandData is null)
                throw new System.ArgumentNullException(nameof(commandData));

            var uiapp = commandData.Application;
            var exporter = new FamilyExporter();

            using (var asd = new OpenFileDialog { Multiselect = true, CheckFileExists = true, Filter = "Revit Family Files|*.rfa" })
            {
                if (asd.ShowDialog() != DialogResult.OK)
                    return Result.Cancelled;

                var projPath = Path.ChangeExtension(Path.GetTempFileName(), ".rvt");
                var docWrapper = uiapp.Application.NewProjectDocument(UnitSystem.Metric);
                using (var saveAsOptions = new SaveAsOptions { OverwriteExistingFile = true })
                {
                    docWrapper.SaveAs(projPath, saveAsOptions);
                    docWrapper.Close(false);
                }

                docWrapper = uiapp.OpenAndActivateDocument(projPath).Document;

                var viewType3D = docWrapper.CreateTweakedView3D();
                uiapp.ActiveUIDocument.ActiveView = viewType3D;

                foreach (var rfaPath in asd.FileNames)
                {
                    if (!File.Exists(rfaPath))
                        continue;

                    exporter.ExportFile(docWrapper, viewType3D, rfaPath);
                }
            }

            return Result.Succeeded;
        }
    }
}
