using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Linq;

namespace CodeCave.Threejs.Revit.Exporter
{
    public class FamilyExporter
    {
        public void ExportFile(Document docWrapper, View3D view3d, string rfaFilePath)
        {
            if (docWrapper is null)
                throw new ArgumentNullException(nameof(docWrapper));

            if (docWrapper.IsFamilyDocument)
                throw new InvalidOperationException("Only project-type documents could be exported.");

            var familyName = Path.GetFileNameWithoutExtension(rfaFilePath);
            var familyExportArgs = new FamilyExportEventArgs { FamilyFilePath = rfaFilePath };

            this?.OnExportStarted(familyExportArgs);

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
                    throw new InvalidOperationException();

                var familyTypeExportArgs = new FamilySymbolExportEventArgs { FamilyFilePath = rfaFilePath, Symbol = familySymbol?.Name };

                this?.OnSymbolExportStarted(familyTypeExportArgs);

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

                var context = new ObjectSceneExportContext(docWrapper, view3d, new FileInfo(outputFilePath));
                using (var exporter = new CustomExporter(docWrapper, context)
                {
                    ShouldStopOnError = false,
                })
                {
                    exporter.Export(view3d as View);
                }

                this?.OnSymbolExportEnded(familyTypeExportArgs);

                using (var t = new Transaction(docWrapper, $"Remove family symbol '{familyInstance.Name}'"))
                {
                    t.Start();
                    docWrapper.Delete(familySymbol.Id);
                    t.Commit();
                }
            }

            this?.OnExportEnded(familyExportArgs);

            using (var t = new Transaction(docWrapper, $"Remove family '{family.Name}'"))
            {
                t.Start();
                docWrapper.Delete(family.Id);
                t.Commit();
            }
        }

        #region Events

        public delegate void FamilyExportEvent(FamilyExporter exporter, FamilyExportEventArgs args);
        public delegate void FamilySymbolExportEvent(FamilyExporter exporter, FamilySymbolExportEventArgs args);

        private event FamilyExportEvent familyExportStarted, familyExportEnded;
        private event FamilySymbolExportEvent symbolExportStarted, symbolExportEnded;

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

        protected virtual void OnExportStarted(FamilyExportEventArgs args)
        {
            FamilyExportEvent handler = familyExportStarted;
            handler?.Invoke(this, args);
        }

        protected virtual void OnExportEnded(FamilyExportEventArgs args)
        {
            FamilyExportEvent handler = familyExportEnded;
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

        #endregion Events
    }
}
