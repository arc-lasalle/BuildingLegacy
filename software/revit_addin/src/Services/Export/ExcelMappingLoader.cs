using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BLComponentTemplate.Models;

namespace BLComponentTemplate.Services.Export
{
    public static class ExcelMappingLoader
    {
        public static List<ExcelMappingRule> LoadMappings(string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
                throw new ArgumentException("La ruta del fichero de mapeo no es válida.", nameof(jsonPath));

            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("No se ha encontrado el fichero de mapeo JSON.", jsonPath);

            string json = File.ReadAllText(jsonPath);

            List<ExcelMappingRule> mappings = JsonSerializer.Deserialize<List<ExcelMappingRule>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return mappings ?? new List<ExcelMappingRule>();
        }
    }
}