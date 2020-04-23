using Autodesk.Revit.DB;

namespace CodeCave.Threejs.Revit.Exporter
{
    public static class ElementExtensions
    {
        /// <summary>
        ///     Gets the description of the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public static string GetDescription(this Element element)
        {
            if (element == null) return "<null>";

            var typeName = element.GetType().Name;
            var categoryName = element.Category?.Name ?? string.Empty;

            var fi = element as FamilyInstance;
            var symbolName = fi?.Symbol?.Name;
            var familyName = fi?.Symbol?.Family?.Name ?? string.Empty;

            var description = Equals(element.Name, typeName)
                ? $"{typeName} {categoryName} {familyName}"
                : $"{typeName} {categoryName} {familyName} {symbolName}";

            return $"{description.Replace("  ", " ")} <{element.Id?.IntegerValue ?? 0} {element.Name}>";
        }
    }
}
