using System.Collections.Generic;

namespace BLComponentTemplate.Utils
{
    public static class MaterialNameLocalizationService
    {
        private static readonly Dictionary<string, string> EnglishToSpanish =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "Stainless Steel", "Acero inoxidable" },
                { "Steel", "Acero" },
                { "Aluminum", "Aluminio" },
                { "Aluminium", "Aluminio" },
                { "Glass", "Vidrio" },
                { "Wood", "Madera" },
                { "Timber", "Madera" },
                { "Concrete", "Hormigón" },
                { "Insulation", "Aislamiento" },
                { "Mineral Wool", "Lana mineral" },
                { "Gypsum", "Yeso" },
                { "Plastic", "Plástico" },
                { "PVC", "PVC" },
                { "Paint", "Pintura" }
            };

        public static string ToSpanish(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                return materialName;

            return EnglishToSpanish.TryGetValue(materialName.Trim(), out string translated)
                ? translated
                : materialName;
        }
    }
}
