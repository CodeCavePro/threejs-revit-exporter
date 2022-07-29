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
    public class RvtExporterCommand : IExternalCommand
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

            var exporter = new FamilyExporter();

            using (var dialog = new OpenFileDialog { Multiselect = false, CheckFileExists = true, Filter = "Revit Project Files|*.rvt" })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return Result.Cancelled;

                exporter.ExportRvtFile(commandData.Application, dialog.FileName);
            }

            return Result.Succeeded;
        }
    }
}
