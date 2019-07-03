using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Linq;

namespace CodeCave.Revit.Threejs.Exporter.Addin
{
    public class Exporter
    {
        protected UIApplication uiapp;

        public Exporter(UIApplication uiapp)
        {
            this.uiapp = uiapp;
        }

        public void ExportFile(Document docWrapper, string rfaFilePath)
        {
            var familyName = Path.GetFileNameWithoutExtension(rfaFilePath);
            var familyExportArgs = new FamilyExportEventArgs { FamilyPath = rfaFilePath };

            this?.OnSymbolExportStarted(familyExportArgs);

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

                var familyTypeExportArgs = new FamilyExportEventArgs { FamilyPath = rfaFilePath, Symbol = familySymbol?.Name };

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

                var context = new ObjectSceneExportContext(docWrapper, new FileInfo(outputFilePath));
                var exporter = new CustomExporter(docWrapper, context)
                {
                    ShouldStopOnError = false
                };

                exporter.Export(uiapp.ActiveUIDocument.ActiveView as View3D);

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

        public delegate void FamilyExportEvent(Exporter exporter, FamilyExportEventArgs args);
        public delegate void FamilySymbolExportEvent(Exporter exporter, FamilyExportEventArgs args);

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

        protected virtual void OnSymbolExportStarted(FamilyExportEventArgs args)
        {
            FamilySymbolExportEvent handler = symbolExportStarted;
            handler?.Invoke(this, args);
        }

        protected virtual void OnSymbolExportEnded(FamilyExportEventArgs args)
        {
            FamilySymbolExportEvent handler = symbolExportEnded;
            handler?.Invoke(this, args);
        }

        #endregion Events
    }

    public class FamilyExportEventArgs
    {
        public string FamilyPath { get; set; }

        public string FamilyName => string.IsNullOrWhiteSpace(FamilyName) ? string.Empty : Path.GetFileNameWithoutExtension(FamilyPath);

        public string Symbol { get; set; }
    }
}
