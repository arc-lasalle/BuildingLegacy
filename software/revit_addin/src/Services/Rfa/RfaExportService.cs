using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;

namespace BLComponentTemplate.Services.Rfa
{
    public static class RfaExportService
    {
        public static void Export(Document projectDoc, ComponentMatch selectedMatch, string outputFilePath)
        {
            if (projectDoc == null)
                throw new ArgumentNullException(nameof(projectDoc));

            if (selectedMatch == null)
                throw new ArgumentNullException(nameof(selectedMatch));

            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("La ruta de salida RFA no es válida.", nameof(outputFilePath));

            Family family = FindFamily(projectDoc, selectedMatch);

            if (family == null)
                throw new InvalidOperationException("No se ha encontrado la familia del componente seleccionado.");

            if (family.IsInPlace)
                throw new InvalidOperationException("La familia seleccionada es in-place y no puede exportarse como .rfa.");

            if (!family.IsEditable)
                throw new InvalidOperationException("La familia seleccionada no es editable y no puede exportarse como .rfa.");

            // EditFamily no puede llamarse con el documento modifiable ni en read-only.
            Document familyDoc = projectDoc.EditFamily(family);

            try
            {
                SaveAsOptions saveOptions = new SaveAsOptions
                {
                    OverwriteExistingFile = true
                };

                familyDoc.SaveAs(outputFilePath, saveOptions);
            }
            finally
            {
                // No queremos guardar cambios dentro del editor de familia al cerrar.
                if (familyDoc != null && familyDoc.IsValidObject)
                {
                    familyDoc.Close(false);
                }
            }
        }

        private static Family FindFamily(Document projectDoc, ComponentMatch selectedMatch)
        {
            // Buscar primero por FamilyInstance y nombre de familia/tipo
            FamilyInstance matchingInstance = new FilteredElementCollector(projectDoc)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .FirstOrDefault(fi =>
                    fi.Symbol != null &&
                    fi.Symbol.Family != null &&
                    string.Equals(fi.Symbol.Family.Name, selectedMatch.Family, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(fi.Symbol.Name, selectedMatch.TypeName, StringComparison.OrdinalIgnoreCase));

            if (matchingInstance?.Symbol?.Family != null)
                return matchingInstance.Symbol.Family;

            // Fallback: por nombre de familia
            Family family = new FilteredElementCollector(projectDoc)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .FirstOrDefault(f =>
                    string.Equals(f.Name, selectedMatch.Family, StringComparison.OrdinalIgnoreCase));

            return family;
        }
    }
}
