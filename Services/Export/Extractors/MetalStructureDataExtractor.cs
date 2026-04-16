using Autodesk.Revit.DB;
using BLComponentTemplate.Models;
using BLComponentTemplate.Services.Revit;
using BLComponentTemplate.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BLComponentTemplate.Services.Export.Extractors
{
    public class MetalStructureDataExtractor : IComponentDataExtractor
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
                "Estructura metálica compuesta",
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
                .SelectMany(m => m.InstanceElementIds)
                .Distinct()
                .ToList();

            return ids
                .Select(id => doc.GetElement(new ElementId(id)))
                .Where(e => e != null)
                .ToList();
        }

        private static string BuildEnvelopeDimensionsText(List<Element> elements, string targetUnit)
        {
            BoundingBoxXYZ bb = BuildEnvelopeBoundingBox(elements);
            if (bb == null)
                return null;

            double width = bb.Max.X - bb.Min.X;
            double length = bb.Max.Y - bb.Min.Y;
            double height = bb.Max.Z - bb.Min.Z;

            string w = ConvertLength(width, targetUnit);
            string l = ConvertLength(length, targetUnit);
            string h = ConvertLength(height, targetUnit);

            return $"W={w}, H={h}, L={l}";
        }

        private static string BuildEnvelopeVolumeText(List<Element> elements, string targetUnit)
        {
            BoundingBoxXYZ bb = BuildEnvelopeBoundingBox(elements);
            if (bb == null)
                return null;

            double width = bb.Max.X - bb.Min.X;
            double length = bb.Max.Y - bb.Min.Y;
            double height = bb.Max.Z - bb.Min.Z;

            double volumeInternal = width * length * height;
            double converted = targetUnit switch
            {
                "mm3" => UnitUtils.ConvertFromInternalUnits(volumeInternal, UnitTypeId.CubicMillimeters),
                "cm3" => UnitUtils.ConvertFromInternalUnits(volumeInternal, UnitTypeId.CubicCentimeters),
                "m3" => UnitUtils.ConvertFromInternalUnits(volumeInternal, UnitTypeId.CubicMeters),
                _ => UnitUtils.ConvertFromInternalUnits(volumeInternal, UnitTypeId.CubicMeters)
            };

            return converted.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static BoundingBoxXYZ BuildEnvelopeBoundingBox(List<Element> elements)
        {
            if (elements == null || elements.Count == 0)
                return null;

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            bool any = false;

            foreach (Element e in elements)
            {
                BoundingBoxXYZ bb = e.get_BoundingBox(null);
                if (bb == null)
                    continue;

                any = true;

                minX = Math.Min(minX, bb.Min.X);
                minY = Math.Min(minY, bb.Min.Y);
                minZ = Math.Min(minZ, bb.Min.Z);

                maxX = Math.Max(maxX, bb.Max.X);
                maxY = Math.Max(maxY, bb.Max.Y);
                maxZ = Math.Max(maxZ, bb.Max.Z);
            }

            if (!any)
                return null;

            return new BoundingBoxXYZ
            {
                Min = new XYZ(minX, minY, minZ),
                Max = new XYZ(maxX, maxY, maxZ)
            };
        }

        private static string ConvertLength(double valueInternal, string targetUnit)
        {
            double converted = targetUnit switch
            {
                "mm" => UnitUtils.ConvertFromInternalUnits(valueInternal, UnitTypeId.Millimeters),
                "cm" => UnitUtils.ConvertFromInternalUnits(valueInternal, UnitTypeId.Centimeters),
                "m" => UnitUtils.ConvertFromInternalUnits(valueInternal, UnitTypeId.Meters),
                _ => UnitUtils.ConvertFromInternalUnits(valueInternal, UnitTypeId.Millimeters)
            };

            return converted.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string BuildInternalStructureText(List<ComponentMatch> selectedMatches)
        {
            var grouped = selectedMatches
                .GroupBy(m => NormalizeStructuralLabel(m.Category))
                .Select(g => $"{g.Sum(x => x.InstanceCount)} {g.Key}")
                .ToList();

            return grouped.Count == 0 ? null : string.Join("; ", grouped);
        }

        private static string NormalizeStructuralLabel(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "elementos";

            string c = category.Trim().ToLowerInvariant();

            if (c.Contains("column") || c.Contains("columna"))
                return "pilares";

            if (c.Contains("framing") || c.Contains("beam") || c.Contains("viga"))
                return "vigas";

            return "elementos";
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