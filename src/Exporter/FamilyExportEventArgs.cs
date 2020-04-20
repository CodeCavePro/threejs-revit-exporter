using System;
using System.Collections.Generic;
using System.IO;

namespace SpecifiGlobal.Revit.Headless.Exporter
{
    public class FamilyExportEventArgs
    {
        public string FamilyFilePath { get; set; }

        public string FamilyName => string.IsNullOrWhiteSpace(FamilyName) ? string.Empty : Path.GetFileNameWithoutExtension(FamilyFilePath);

        public string Symbol { get; set; }

        public Dictionary<Exception, string> Exceptions { get; internal set; }
    }
}
