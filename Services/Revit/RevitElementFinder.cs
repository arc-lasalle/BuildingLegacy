using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using BLComponentTemplate.Models;
using BLComponentTemplate.Others;
using BLComponentTemplate.Utils;

namespace BLComponentTemplate.Services.Revit
{
    public static class RevitElementFinder
    {
        public static List<ComponentMatch> FindMatches(Document doc, string componentType, bool preciseMode)
        {
            ComponentSearchRule rule = ComponentSearchRuleResolver.GetRule(componentType);

            if (rule == null || rule.Categories == null || rule.Categories.Count == 0)
                return new List<ComponentMatch>();

            List<Element> allElements = new List<Element>();

            foreach (BuiltInCategory bic in rule.Categories)
            {
                var elements = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .WhereElementIsNotElementType()
                    .ToElements();

                allElements.AddRange(elements);
            }

            List<Element> filteredElements;

            if (preciseMode)
            {
                filteredElements = allElements
                    .Where(e => MatchesRule(e, rule))
                    .ToList();
            }
            else
            {
                filteredElements = allElements;
            }

            return GroupElementsByType(filteredElements, componentType);
        }

        private static bool MatchesRule(Element element, ComponentSearchRule rule)
        {
            ElementType elementType = element.Document.GetElement(element.GetTypeId()) as ElementType;

            string familyName = elementType?.FamilyName ?? "";
            string typeName = elementType?.Name ?? "";
            string elementName = element.Name ?? "";
            string categoryName = element.Category?.Name ?? "";
            string className = element.GetType().Name ?? "";

            bool hasFamilyFilters = rule.FamilyNameContainsAny != null && rule.FamilyNameContainsAny.Count > 0;
            bool hasTypeFilters = rule.TypeNameContainsAny != null && rule.TypeNameContainsAny.Count > 0;
            bool hasElementNameFilters = rule.ElementNameContainsAny != null && rule.ElementNameContainsAny.Count > 0;
            bool hasCategoryNameFilters = rule.CategoryNameContainsAny != null && rule.CategoryNameContainsAny.Count > 0;
            bool hasClassNameFilters = rule.ClassNameContainsAny != null && rule.ClassNameContainsAny.Count > 0;
            bool hasParamNameFilters = rule.ParameterNameContainsAny != null && rule.ParameterNameContainsAny.Count > 0;
            bool hasParamValueFilters = rule.ParameterValueContainsAny != null && rule.ParameterValueContainsAny.Count > 0;

            bool familyMatch = !hasFamilyFilters || ContainsAny(familyName, rule.FamilyNameContainsAny);
            bool typeMatch = !hasTypeFilters || ContainsAny(typeName, rule.TypeNameContainsAny);
            bool elementNameMatch = !hasElementNameFilters || ContainsAny(elementName, rule.ElementNameContainsAny);
            bool categoryNameMatch = !hasCategoryNameFilters || ContainsAny(categoryName, rule.CategoryNameContainsAny);
            bool classNameMatch = !hasClassNameFilters || ContainsAny(className, rule.ClassNameContainsAny);
            bool parameterNameMatch = !hasParamNameFilters || MatchesAnyParameterName(element, elementType, rule.ParameterNameContainsAny);
            bool parameterValueMatch = !hasParamValueFilters || MatchesAnyParameterValue(element, elementType, rule.ParameterValueContainsAny);

            return familyMatch
                && typeMatch
                && elementNameMatch
                && categoryNameMatch
                && classNameMatch
                && parameterNameMatch
                && parameterValueMatch;
        }

        private static bool MatchesAnyParameterName(Element instance, ElementType type, List<string> keywords)
        {
            return ParameterNames(instance).Concat(ParameterNames(type)).Any(name => ContainsAny(name, keywords));
        }

        private static bool MatchesAnyParameterValue(Element instance, ElementType type, List<string> keywords)
        {
            return ParameterValues(instance).Concat(ParameterValues(type)).Any(value => ContainsAny(value, keywords));
        }

        private static IEnumerable<string> ParameterNames(Element element)
        {
            if (element == null)
                yield break;

            foreach (Parameter p in element.Parameters)
            {
                if (p?.Definition?.Name != null)
                    yield return p.Definition.Name;
            }
        }

        private static IEnumerable<string> ParameterValues(Element element)
        {
            if (element == null)
                yield break;

            foreach (Parameter p in element.Parameters)
            {
                string value = GetParameterValueAsString(p, element.Document);
                if (!string.IsNullOrWhiteSpace(value))
                    yield return value;
            }
        }

        private static string GetParameterValueAsString(Parameter p, Document doc)
        {
            if (p == null || !p.HasValue)
                return string.Empty;

            try
            {
                switch (p.StorageType)
                {
                    case StorageType.String:
                        return p.AsString() ?? "";

                    case StorageType.Integer:
                        return p.AsInteger().ToString();

                    case StorageType.Double:
                        return p.AsValueString() ?? "";

                    case StorageType.ElementId:
                        ElementId id = p.AsElementId();
                        Element referenced = doc.GetElement(id);
                        return referenced != null ? referenced.Name : id.Value.ToString();

                    default:
                        return "";
                }
            }
            catch
            {
                return "";
            }
        }

        private static bool ContainsAny(string source, List<string> keywords)
        {
            if (string.IsNullOrWhiteSpace(source) || keywords == null || keywords.Count == 0)
                return false;

            string normalized = source.ToLowerInvariant();

            return keywords.Any(k =>
                !string.IsNullOrWhiteSpace(k) &&
                normalized.Contains(k.ToLowerInvariant()));
        }

        private static List<ComponentMatch> GroupElementsByType(List<Element> elements, string componentType)
        {
            return elements
                .Where(e => e.Category != null)
                .GroupBy(e =>
                {
                    ElementType elementType = e.Document.GetElement(e.GetTypeId()) as ElementType;

                    string familyName = elementType?.FamilyName ?? "";
                    string typeName = elementType?.Name ?? "";
                    string typeId = elementType != null ? elementType.Id.Value.ToString() : "";

                    string dimensionsSignature = string.Equals(componentType, "Vidrios", StringComparison.OrdinalIgnoreCase)
                        ? BuildGlassDimensionsSignature(e, e.Document)
                        : BuildDimensionsSignature(e, elementType, e.Document);

                    return new
                    {
                        Category = e.Category.Name,
                        Family = familyName,
                        TypeName = typeName,
                        TypeId = typeId,
                        DimensionsSignature = dimensionsSignature
                    };
                })
                .Select(g => new ComponentMatch
                {
                    Category = g.Key.Category,
                    Family = g.Key.Family,
                    TypeName = g.Key.TypeName,
                    ElementId = g.Key.TypeId,
                    DimensionsSignature = g.Key.DimensionsSignature,
                    InstanceCount = g.Count(),
                    InstanceElementIds = g.Select(static e => (int)e.Id.Value).ToList(),
                    IsIncludedInAssembly = true
                })
                .OrderBy(m => m.Category)
                .ThenBy(m => m.Family)
                .ThenBy(m => m.TypeName)
                .ThenBy(m => m.DimensionsSignature)
                .ToList();
        }

        private static string BuildGlassDimensionsSignature(Element element, Document doc)
        {
            if (element == null)
                return "(sin dimensiones)";

            BoundingBoxXYZ bb = element.get_BoundingBox(null);
            if (bb == null)
                return "(sin dimensiones)";

            double widthInternal = bb.Max.X - bb.Min.X;
            double heightInternal = bb.Max.Z - bb.Min.Z;
            double depthInternal = bb.Max.Y - bb.Min.Y;

            double widthMm = UnitUtils.ConvertFromInternalUnits(widthInternal, UnitTypeId.Millimeters);
            double heightMm = UnitUtils.ConvertFromInternalUnits(heightInternal, UnitTypeId.Millimeters);
            double depthMm = UnitUtils.ConvertFromInternalUnits(depthInternal, UnitTypeId.Millimeters);

            // Redondeo para evitar diferencias insignificantes por tolerancias geométricas
            widthMm = Math.Round(widthMm, 1);
            heightMm = Math.Round(heightMm, 1);
            depthMm = Math.Round(depthMm, 1);

            return $"W={widthMm:0.#}, H={heightMm:0.#}, D={depthMm:0.#}";
        }

        private static string BuildDimensionsSignature(Element instance, ElementType elementType, Document doc)
        {
            string width = FindDimensionValue(instance, elementType, doc,
                new[] { "Width", "Anchura", "Ancho", "Frame Width", "Rough Width" });

            string height = FindDimensionValue(instance, elementType, doc,
                new[] { "Height", "Altura", "Frame Height", "Rough Height" });

            string thickness = FindDimensionValue(instance, elementType, doc,
                new[] { "Thickness", "Espesor", "Depth", "Profundidad" });

            List<string> parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(width))
                parts.Add($"W={width}");

            if (!string.IsNullOrWhiteSpace(height))
                parts.Add($"H={height}");

            if (!string.IsNullOrWhiteSpace(thickness))
                parts.Add($"T={thickness}");

            return parts.Count == 0 ? "(sin dimensiones)" : string.Join(", ", parts);
        }

        private static string FindDimensionValue(
            Element instance,
            ElementType elementType,
            Document doc,
            IEnumerable<string> candidateNames)
        {
            string fromInstance = RevitParameterSearchService.FindFirstParameterValue(instance, candidateNames, doc);
            if (!string.IsNullOrWhiteSpace(fromInstance))
                return fromInstance;

            string fromType = RevitParameterSearchService.FindFirstParameterValue(elementType, candidateNames, doc);
            if (!string.IsNullOrWhiteSpace(fromType))
                return fromType;

            return null;
        }
    }
}