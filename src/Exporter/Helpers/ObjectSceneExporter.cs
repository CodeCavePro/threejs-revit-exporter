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
        /// <param name="outputJsonFilePath">The output JSON file path.</param>
        /// <exception cref="System.ArgumentException">Please provide a valid output file path. - outputJsonFilePath.</exception>
        public bool ExportToFile(View3D viewToExport, string outputJsonFilePath)
        {
            if (viewToExport is null)
            {
                throw new ArgumentNullException(nameof(viewToExport));
            }

            if (string.IsNullOrWhiteSpace(outputJsonFilePath))
            {
                throw new ArgumentException("Please provide a valid output file path.", nameof(outputJsonFilePath));
            }

            _ = new FileInfo(outputJsonFilePath); // throws an error if file path is invalid

            if (!TryExport(viewToExport, out var outputScene, throwException: true))
            {
                return false;
            }

            File.WriteAllText(outputJsonFilePath, outputScene.ToString());
            return File.Exists(outputJsonFilePath);
        }

        /// <summary>Exports the specified view to export.</summary>
        /// <param name="viewToExport">The view to export.</param>
        /// <param name="outputScene">The output scene.</param>
        /// <param name="throwException">Throw or not throw exception, default is true.</param>
        public bool TryExport(View3D viewToExport, out ObjectScene outputScene, bool throwException = false)
        {
            outputScene = null;

            try
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

                outputScene = this.context.GetResult();
                return outputScene?.Object != null;
            }
            catch when (!throwException)
            {
                return false;
            }
        }
    }
}
