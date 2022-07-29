using System;
using System.IO;
using Autodesk.Revit.DB;
using CodeCave.Threejs.Entities;

namespace CodeCave.Threejs.Revit.Exporter
{
    public class ObjectSceneExporter : CustomExporter
    {
        private readonly ObjectSceneExportContext context;

        public ObjectSceneExporter(Document document, ObjectSceneExportContext context)
            : base(document, context)
        {
            this.context = context;
        }

        /// <summary>Exports the specified view to export.</summary>
        /// <param name="viewToExport">The view to export.</param>
        /// <returns></returns>
        public ObjectScene ExportView(View viewToExport)
        {
            if (viewToExport is null)
            {
                throw new ArgumentNullException(nameof(viewToExport));
            }

#if NET472 || NET48_OR_GREATER
            Export(viewToExport);
#else
            Export(new System.Collections.Generic.List<ElementId> { viewToExport.Id });
#endif
            return context.GetResult();
        }

        /// <summary>Exports the specified view to export.</summary>
        /// <param name="viewToExport">The view to export.</param>
        /// <param name="outputJsonFilePath">The output json file path.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Please provide a valid output file path. - outputJsonFilePath.</exception>
        public bool Export(View viewToExport, string outputJsonFilePath)
        {
            if (string.IsNullOrEmpty(outputJsonFilePath))
                throw new ArgumentException("Please provide a valid output file path.", nameof(outputJsonFilePath));

            _ = new FileInfo(outputJsonFilePath); // throws an error if file path is invalid
            var outputScene = ExportView(viewToExport);
            var outPutJson = outputScene.ToString();

            File.WriteAllText(outputJsonFilePath, outPutJson);
            return File.Exists(outPutJson);
        }
    }
}
