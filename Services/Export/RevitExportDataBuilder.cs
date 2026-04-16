using Autodesk.Revit.DB;
using Autodesk.Revit.DB;
using BLComponentTemplate.Models;
using BLComponentTemplate.Services.Export.Extractors;
using BLComponentTemplate.Services.Revit;
using BLComponentTemplate.Utils;
using System.Collections.Generic;
using System.Globalization;
using System.Globalization;
using System.Linq;

namespace BLComponentTemplate.Services.Export
{
    public static class RevitExportDataBuilder
    {
        public static List<ExportableParameter> Build(
            Document doc,
            string elementScale,
            string componentType,
            ComponentMatch selectedMatch,
            List<ComponentMatch> selectedMatches,
            string dimensionsUnit,
            string areaOrVolumeUnit)
        {
            var results = new List<ExportableParameter>();

            if (doc == null || selectedMatch == null)
                return results;

            RevitTypeContext context = RevitTypeContextBuilder.Build(doc, selectedMatch);
            if (context == null && componentType != "Estructuras metálicas")
                return results;            

            bool compositeExportMode =
                (componentType == "Estructuras metálicas" || componentType == "Sistemas de climatización")
                && selectedMatches != null
                && selectedMatches.Count > 1;

            if (selectedMatches != null && selectedMatches.Count == 1 && componentType == "Estructuras metálicas")
            {
                compositeExportMode = false;
            }

            bool isSystem = string.Equals(elementScale, "Sistema", StringComparison.OrdinalIgnoreCase);

            IComponentDataExtractor extractor =
                ComponentDataExtractorResolver.Resolve(componentType, isSystem);

            return extractor.Extract(
                doc,
                context,
                componentType,
                selectedMatch,
                selectedMatches,
                dimensionsUnit,
                areaOrVolumeUnit);
        }

        public static bool IsValidAddressText(string value)
        {
            if (!IsReasonableText(value, allowNumericOnly: false, minLength: 5))
                return false;

            string normalized = value.Trim().ToLowerInvariant();

            string[] invalidPlaceholders =
            {
                "introduzca la dirección aquí",
                "introduzca la direccion aqui",
                "enter address here",
                "internal",
                "interno",
                "n/a",
                "na",
                "none",
                "sin dirección",
                "sin direccion"
            };

            return !invalidPlaceholders.Contains(normalized);
        }        
        
        public static bool IsReasonableText(string value, bool allowNumericOnly, int minLength = 2)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string trimmed = value.Trim();

            if (trimmed.Length < minLength)
                return false;

            if (!allowNumericOnly && trimmed.All(char.IsDigit))
                return false;

            return true;
        }       
        
        public static string BuildDimensionsText(RevitTypeContext context, string targetUnit)
        {
            if (context?.Instances == null || context.Instances.Count == 0)
                return null;

            var uniqueLines = new HashSet<string>();

            foreach (Element instance in context.Instances)
            {
                Parameter widthParam = FindDimensionParameter(instance, context.ElementType,
                    new[] { "Width", "Anchura", "Ancho", "Frame Width", "Rough Width" });

                Parameter heightParam = FindDimensionParameter(instance, context.ElementType,
                    new[] { "Height", "Altura", "Frame Height", "Rough Height" });

                Parameter thicknessParam = FindDimensionParameter(instance, context.ElementType,
                    new[] { "Thickness", "Espesor", "Depth", "Profundidad" });

                List<string> parts = new List<string>();

                string width = ConvertLengthParameter(widthParam, targetUnit);
                string height = ConvertLengthParameter(heightParam, targetUnit);
                string thickness = ConvertLengthParameter(thicknessParam, targetUnit);

                if (!string.IsNullOrWhiteSpace(width))
                    parts.Add($"W={width}");

                if (!string.IsNullOrWhiteSpace(height))
                    parts.Add($"H={height}");

                if (!string.IsNullOrWhiteSpace(thickness))
                    parts.Add($"T={thickness}");

                if (parts.Count > 0)
                {
                    uniqueLines.Add(string.Join(", ", parts));
                }
            }

            return uniqueLines.Count == 0 ? null : string.Join("; ", uniqueLines);
        }

        public static string BuildAreaVolumeText(RevitTypeContext context, string targetUnit)
        {
            if (context?.Instances == null || context.Instances.Count == 0)
                return null;

            var uniqueValues = new HashSet<string>();

            foreach (Element instance in context.Instances)
            {
                Parameter areaParam = RevitParameterSearchService.FindFirstParameter(
                    instance,
                    new[] { "Area", "Host Area Computed", "Computed Area", "Superficie" });

                Parameter volumeParam = RevitParameterSearchService.FindFirstParameter(
                    instance,
                    new[] { "Volume", "Volumen" });

                string converted = null;

                if (volumeParam != null)
                {
                    converted = ConvertVolumeParameter(volumeParam, targetUnit);
                }
                else if (areaParam != null)
                {
                    converted = ConvertAreaParameter(areaParam, targetUnit);
                }

                if (!string.IsNullOrWhiteSpace(converted))
                    uniqueValues.Add(converted);
            }

            return uniqueValues.Count == 0 ? null : string.Join("; ", uniqueValues);
        }


        private static Parameter FindDimensionParameter(Element instance, ElementType elementType, IEnumerable<string> candidateNames)
        {
            Parameter fromInstance = RevitParameterSearchService.FindFirstParameter(instance, candidateNames);
            if (fromInstance != null)
                return fromInstance;

            return RevitParameterSearchService.FindFirstParameter(elementType, candidateNames);
        }       
        
        private static string ConvertLengthParameter(Parameter parameter, string targetUnit)
        {
            if (parameter == null || !parameter.HasValue || parameter.StorageType != StorageType.Double)
                return null;

            double value = parameter.AsDouble();
            double converted = targetUnit switch
            {
                "mm" => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Millimeters),
                "cm" => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Centimeters),
                "m" => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Meters),
                _ => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Millimeters)
            };

            return converted.ToString("0.###", CultureInfo.InvariantCulture);
        }
        
        private static string ConvertAreaParameter(Parameter parameter, string targetUnit)
        {
            if (parameter == null || !parameter.HasValue || parameter.StorageType != StorageType.Double)
                return null;

            double value = parameter.AsDouble();
            double converted = targetUnit switch
            {
                "mm2" => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.SquareMillimeters),
                "cm2" => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.SquareCentimeters),
                "m2" => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.SquareMeters),
                _ => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.SquareMeters)
            };

            return converted.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string ConvertVolumeParameter(Parameter parameter, string targetUnit)
        {
            if (parameter == null || !parameter.HasValue || parameter.StorageType != StorageType.Double)
                return null;

            double value = parameter.AsDouble();
            double converted = targetUnit switch
            {
                "mm3" => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.CubicMillimeters),
                "cm3" => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.CubicCentimeters),
                "m3" => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.CubicMeters),
                _ => UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.CubicMeters)
            };

            return converted.ToString("0.###", CultureInfo.InvariantCulture);
        }        
    }
}