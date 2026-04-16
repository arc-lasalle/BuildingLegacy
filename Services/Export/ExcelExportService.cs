using BLComponentTemplate.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BLComponentTemplate.Services.Export
{
    public static class ExcelExportService
    {
        public static void Export(ExportSessionContext context, string outputPath)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrWhiteSpace(context.ImportedTemplatePath))
                throw new InvalidOperationException("No se ha importado ninguna plantilla Excel.");

            if (!File.Exists(context.ImportedTemplatePath))
                throw new FileNotFoundException(
                    "No se encuentra la plantilla Excel importada.",
                    context.ImportedTemplatePath);

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new InvalidOperationException("La ruta de salida no es válida.");

            List<ExportableParameter> selectedParameters = context.ExportableParameters?
                .Where(p => p.IsSelectedForExport)
                .ToList() ?? new List<ExportableParameter>();

            File.Copy(context.ImportedTemplatePath, outputPath, true);

            EnsureLicense();

            using (var package = new ExcelPackage(new FileInfo(outputPath)))
            {
                ExcelTemplateMapper.ApplyMappings(
                    package,
                    selectedParameters);

                package.Save();
            }
        }

        private static void EnsureLicense()
        {
            ExcelPackage.License.SetNonCommercialOrganization("La Salle Campus Barcelona");
        }
    }
}