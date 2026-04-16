using Autodesk.Revit.DB;
using BLComponentTemplate.Models;
using BLComponentTemplate.Services.Revit;
using BLComponentTemplate.Utils;
using System.Collections.Generic;
using System.Linq;

namespace BLComponentTemplate.Services.Export.Extractors
{
    public class HvacSystemDataExtractor : IComponentDataExtractor
    {
        public List<ExportableParameter> Extract(
            Document doc,
            RevitTypeContext context,
            string componentType,
            ComponentMatch selectedMatch,
            List<ComponentMatch> selectedMatches,
            string dimensionsUnit,
            string areaOrVolumeUnit)
        {
            var results = new List<ExportableParameter>();

            if (doc == null || selectedMatches == null || selectedMatches.Count == 0)
                return results;

            List<Element> elements = GetSelectedElements(doc, selectedMatches);
            if (elements.Count == 0)
                return results;

            Add(results, "Nombre del producto",
                "Sistema de climatización compuesto",
                "Generado por el Add-on");

            Add(results, "Ubicación actual del edificio (dirección física)",
                FindBuildingAddress(doc),
                "Información de proyecto");

            Add(results, "Medidas del componente",
                BuildEnvelopeDimensionsText(elements, dimensionsUnit),
                "Envolvente geométrica",
                dimensionsUnit);

            Add(results, "Superficie o volumen del material",
                BuildEnvelopeVolumeText(elements, areaOrVolumeUnit),
                "Envolvente geométrica",
                areaOrVolumeUnit);

            Add(results, "Caracterización / Estructura interna",
                BuildInternalStructureText(selectedMatches),
                "Subcomponentes agrupados");

            string mainMaterial = BuildMainMaterial(elements, doc);
            Add(results, "Material principal",
                mainMaterial,
                "Material Revit");

            string materials = BuildMaterialsText(elements, doc);
            Add(results, "Materiales",
                materials,
                "Material Revit");

            Add(results, "Número de instancias",
                "1",
                "Generado por el Add-on");

            return results
                .Where(r => !string.IsNullOrWhiteSpace(r.Value))
                .ToList();
        }

        private static void Add(List<ExportableParameter> results, string displayName, string value, string source, string unit = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            results.Add(new ExportableParameter
            {
                DisplayName = displayName,
                Value = value,
                Source = source,
                Unit = unit,
                IsSelectedForExport = true
            });
        }

        private static List<Element> GetSelectedElements(Document doc, List<ComponentMatch> selectedMatches)
        {
            var ids = selectedMatches
                .SelectMany(m => m.InstanceElementIds ?? new List<int>())
                .Distinct()
                .ToList();

            return ids
                .Select(id => doc.GetElement(new ElementId(id)))
                .Where(e => e != null)
                .ToList();
        }

        private static string BuildEnvelopeDimensionsText(List<Element> elements, string targetUnit)
        {
            return MetalStructureDataExtractorHelper.BuildEnvelopeDimensionsText(elements, targetUnit);
        }

        private static string BuildEnvelopeVolumeText(List<Element> elements, string targetUnit)
        {
            return MetalStructureDataExtractorHelper.BuildEnvelopeVolumeText(elements, targetUnit);
        }

        private static string BuildInternalStructureText(List<ComponentMatch> selectedMatches)
        {
            var grouped = selectedMatches
                .GroupBy(m => NormalizeHvacLabel(m.Category, m.Family, m.TypeName))
                .Select(g => $"{g.Sum(x => x.InstanceCount)} {g.Key}")
                .ToList();

            return grouped.Count == 0 ? null : string.Join("; ", grouped);
        }

        private static string NormalizeHvacLabel(string category, string family, string typeName)
        {
            string combined = $"{category} {family} {typeName}".ToLowerInvariant();

            if (combined.Contains("duct") || combined.Contains("conduct"))
                return "conductos";

            if (combined.Contains("terminal") || combined.Contains("diffuser") || combined.Contains("rejilla"))
                return "terminales";

            if (combined.Contains("mechanical equipment") || combined.Contains("equipment") || combined.Contains("equipo"))
                return "equipos";

            if (combined.Contains("pipe") || combined.Contains("tuber"))
                return "tuberías";

            return "subcomponentes";
        }

        private static string BuildMainMaterial(List<Element> elements, Document doc)
        {
            List<Material> materials = MaterialExtractionService.GetMaterials(elements, doc);

            return materials.FirstOrDefault() != null
                ? MaterialNameLocalizationService.ToSpanish(materials.First().Name)
                : null;
        }

        private static string BuildMaterialsText(List<Element> elements, Document doc)
        {
            List<Material> materials = MaterialExtractionService.GetMaterials(elements, doc);
            return MaterialExtractionService.GetMaterialNamesAsText(materials);
        }

        private static string FindBuildingAddress(Document doc)
        {
            if (doc == null)
                return null;

            ProjectInfo projectInfo = doc.ProjectInformation;
            if (projectInfo != null)
            {
                string address = projectInfo.Address;
                if (RevitExportDataBuilder.IsValidAddressText(address))
                    return address;

                string projectName = projectInfo.Name;
                if (RevitExportDataBuilder.IsValidAddressText(projectName))
                    return projectName;
            }

            ProjectLocation location = doc.ActiveProjectLocation;
            if (location != null)
            {
                SiteLocation site = location.GetSiteLocation();
                if (site != null)
                {
                    string placeName = site.PlaceName;
                    if (RevitExportDataBuilder.IsValidAddressText(placeName))
                        return placeName;
                }

                if (RevitExportDataBuilder.IsValidAddressText(location.Name))
                    return location.Name;
            }

            return null;
        }
    }
}