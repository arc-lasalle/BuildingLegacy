using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using BLComponentTemplate.Utils;

namespace BLComponentTemplate.Services.Revit
{
    public static class MaterialExtractionService
    {
        public static List<Material> GetMaterials(Element element, Document doc)
        {
            if (element == null || doc == null)
                return new List<Material>();

            ICollection<ElementId> materialIds = element.GetMaterialIds(false);

            return materialIds
                .Select(id => doc.GetElement(id) as Material)
                .Where(m => m != null)
                .ToList();
        }

        public static List<Material> GetMaterials(IEnumerable<Element> elements, Document doc)
        {
            if (elements == null)
                return new List<Material>();

            return elements
                .SelectMany(e => GetMaterials(e, doc))
                .GroupBy(m => m.Id.Value)
                .Select(g => g.First())
                .ToList();
        }

        public static string GetMaterialNamesAsText(IEnumerable<Material> materials)
        {
            if (materials == null)
                return null;

            var names = materials
                .Select(m => MaterialNameLocalizationService.ToSpanish(m.Name))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            return names.Count == 0 ? null : string.Join("; ", names);
        }
    }
}