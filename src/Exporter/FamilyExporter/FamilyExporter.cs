using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace CodeCave.Threejs.Revit.Exporter
{
    public class FamilyExporter
    {
        #region Events

        public delegate void FamilyExportEvent(FamilyExporter exporter, FamilyExportEventArgs args);

        public delegate void FamilySymbolExportEvent(FamilyExporter exporter, FamilySymbolExportEventArgs args);

        public event FamilyExportEvent FamilyExportStarted
        {
            add { familyExportStarted += value; }
            remove { familyExportStarted -= value; }
        }

        public event FamilyExportEvent FamilyExportEnded
        {
            add { familyExportEnded += value; }
            remove { familyExportEnded -= value; }
        }

        public event FamilySymbolExportEvent SymbolExportStarted
        {
            add { symbolExportStarted += value; }
            remove { symbolExportStarted -= value; }
        }

        public event FamilySymbolExportEvent SymbolExportEnded
        {
            add { symbolExportEnded += value; }
            remove { symbolExportEnded -= value; }
        }

        private event FamilyExportEvent familyExportStarted;

        private event FamilyExportEvent familyExportEnded;

        private event FamilySymbolExportEvent symbolExportStarted;

        private event FamilySymbolExportEvent symbolExportEnded;

        #endregion Events

        public void ExportRfaFile(Document docWrapper, View3D view3d, string rfaFilePath)
        {
            if (docWrapper is null)
                throw new ArgumentNullException(nameof(docWrapper));

            if (docWrapper.IsFamilyDocument)
                throw new InvalidOperationException("Only project-type documents could be exported.");

            var familyName = Path.GetFileNameWithoutExtension(rfaFilePath);
            var familyExportArgs = new FamilyExportEventArgs { FamilyFilePath = rfaFilePath };

            this?.OnExportStarted(familyExportArgs);

            Family family;

            using (var collector = new FilteredElementCollector(docWrapper))
            using (var filter = new ElementClassFilter(typeof(Family), false))
            {
                family = collector
                    .WherePasses(filter)
                    .Cast<Family>()
                    .FirstOrDefault(f => f.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));
            }

            if (family is null)
            {
                using var t = new Transaction(docWrapper, $"Load the family '{familyName}'");
                t.Start();
                if (!docWrapper.LoadFamily(rfaFilePath, out family))
                    throw new InvalidOperationException();
                t.Commit();
            }

            foreach (var typElementId in family.GetFamilySymbolIds())
            {
                FamilyInstance familyInstance;
                if (!(docWrapper.GetElement(typElementId) is FamilySymbol familySymbol))
                    throw new InvalidOperationException();

                var outputFilePath = familySymbol.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase)
                    ? Path.ChangeExtension(rfaFilePath, ".json")
                    : Path.ChangeExtension(rfaFilePath, $";{familySymbol.Name}.json");

                var familyTypeExportArgs = new FamilySymbolExportEventArgs { FamilyFilePath = rfaFilePath, Symbol = familySymbol?.Name, OutputFilePath = outputFilePath };
                this?.OnSymbolExportStarted(familyTypeExportArgs);

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
                        StructuralType.NonStructural);

                    t.Commit();
                }

                var context = new ObjectSceneExportContext(docWrapper, view3d);
                using var exporter = new ObjectSceneExporter(docWrapper, context)
                {
                    ShouldStopOnError = false,
                };

                if (!exporter.TryExport(view3d, out var objectScene))
                {
                    throw new InvalidOperationException($"Failed to export object scene to {outputFilePath}");
                }

                var objectSceneJson = objectScene.ToString();
                File.WriteAllText(outputFilePath, objectSceneJson);

                this?.OnSymbolExportEnded(familyTypeExportArgs);

                using (var t = new Transaction(docWrapper, $"Remove family symbol '{familyInstance.Name}'"))
                {
                    t.Start();
                    docWrapper.Delete(familySymbol.Id);
                    t.Commit();
                }

                familyInstance.Dispose();
            }

            this?.OnExportEnded(familyExportArgs);

            using (var t = new Transaction(docWrapper, $"Remove family '{family.Name}'"))
            {
                t.Start();
                docWrapper.Delete(family.Id);
                t.Commit();
            }

            family.Dispose();
        }

        public void ExportRvtFile(UIApplication uiapp, string projectPath)
        {
            if (uiapp is null)
                throw new ArgumentNullException(nameof(uiapp));

            if (string.IsNullOrWhiteSpace(projectPath))
                throw new ArgumentException($"Invalid project path '{projectPath}'.", nameof(projectPath));

            var docWrapper = uiapp.OpenAndActivateDocument(projectPath).Document;
            var viewType3D = docWrapper.CreateTweakedView3D();
            uiapp.ActiveUIDocument.ActiveView = viewType3D;

            var context = new ObjectSceneExportContext(docWrapper, viewType3D)
            {
                Optimize = false,
            };

            using var exporter = new ObjectSceneExporter(docWrapper, context)
            {
                ShouldStopOnError = false,
            };

            var objectScene = exporter.ExportView(viewType3D as View);
            var objectSceneJson = objectScene.ToString();

            var outputFilePath = Path.ChangeExtension(projectPath, ".json");
            File.WriteAllText(outputFilePath, objectSceneJson);
        }

        protected virtual void OnExportStarted(FamilyExportEventArgs args)
        {
            var handler = familyExportStarted;
            handler?.Invoke(this, args);
        }

        protected virtual void OnExportEnded(FamilyExportEventArgs args)
        {
            var handler = familyExportEnded;
            handler?.Invoke(this, args);
        }

        protected virtual void OnSymbolExportStarted(FamilySymbolExportEventArgs args)
        {
            var handler = symbolExportStarted;
            handler?.Invoke(this, args);
        }

        protected virtual void OnSymbolExportEnded(FamilySymbolExportEventArgs args)
        {
            var handler = symbolExportEnded;
            handler?.Invoke(this, args);
        }
    }
}
