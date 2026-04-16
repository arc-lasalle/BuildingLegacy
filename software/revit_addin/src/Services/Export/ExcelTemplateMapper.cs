using System;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;

namespace BLComponentTemplate.Services.Export
{
    public static class ExcelTemplateMapper
    {
        public static void ApplyMappings(
            ExcelPackage package,
            List<ExportableParameter> selectedParameters)
        {
            if (package == null || selectedParameters == null || selectedParameters.Count == 0)
                return;

            ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                return;

            var parameterDictionary = selectedParameters
                .Where(p => !string.IsNullOrWhiteSpace(p.DisplayName))
                .GroupBy(p => Normalize(p.DisplayName))
                .ToDictionary(
                    g => g.Key,
                    g => g.First());

            int startRow = worksheet.Dimension?.Start.Row ?? 1;
            int endRow = worksheet.Dimension?.End.Row ?? 200;

            for (int row = startRow; row <= endRow; row++)
            {
                string parameterNameInExcel = worksheet.Cells[row, 1].Text?.Trim();

                if (string.IsNullOrWhiteSpace(parameterNameInExcel))
                    continue;

                string normalizedName = Normalize(parameterNameInExcel);

                if (!parameterDictionary.TryGetValue(normalizedName, out ExportableParameter parameter))
                    continue;

                worksheet.Cells[row, 2].Value = parameter.Value ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(parameter.Unit))
                {
                    worksheet.Cells[row, 3].Value = parameter.Unit;
                }
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim().ToLowerInvariant();
        }
    }
}