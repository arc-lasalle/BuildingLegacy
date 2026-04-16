using System;
using System.IO;

namespace BLComponentTemplate.Others
{
    public static class ExcelTemplateResolver
    {
        public static string GetTemplatePath(string componentType)
        {
            string baseFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "Templates");

            string fileName = componentType switch
            {
                "Puertas cortafuegos" => "PuertasCortafuegos.xlsx",
                "Estructuras metálicas" => "EstructurasMetalicas.xlsx",
                "Ventanas" => "Ventanas.xlsx",
                "Lonas" => "Lonas.xlsx",
                "Sistemas de climatización" => "SistemasClimatizacion.xlsx",
                "Puerta enrollable" => "PuertaEnrollables.xlsx",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return Path.Combine(baseFolder, fileName);
        }
    }
}