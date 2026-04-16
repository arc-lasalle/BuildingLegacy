using Autodesk.Revit.DB;
using BLComponentTemplate.Models;
using BLComponentTemplate.Services.Revit;
using BLComponentTemplate.Utils;
using System.Collections.Generic;
using System.Linq;

namespace BLComponentTemplate.Services.Export.Extractors
{
    public class HvacDataExtractor : IComponentDataExtractor
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

            if (context == null || selectedMatch == null)
                return results;

            Add(results, "Nombre del producto",
                FindProductName(context),
                "Parámetro de identidad / Revit");

            Add(results, "Marca o Fabricante",
                FindManufacturer(context),
                "Parámetro Revit");

            Add(results, "Nombre del modelo de producto o referencia",
                FindFirst(context,
                    new[] { "Model", "Modelo", "Type Mark", "Reference", "Referencia", "Model Number" }),
                "Parámetro Revit");

            Add(results, "Ubicación actual del edificio (dirección física)",
                FindBuildingAddress(context.Document),
                "Información de proyecto");

            Add(results, "Medidas del componente",
                RevitExportDataBuilder.BuildDimensionsText(context, dimensionsUnit),
                "Instancias / Tipo Revit",
                dimensionsUnit);

            Add(results, "Color del componente",
                FindFirst(context, new[] { "Color", "Colour" }),
                "Parámetro / Material");

            Add(results, "Acabado del componente",
                FindFirst(context, new[] { "Finish", "Acabado" }),
                "Parámetro Revit");

            List<Material> materials = MaterialExtractionService.GetMaterials(context.Instances, context.Document);
            string materialList = MaterialExtractionService.GetMaterialNamesAsText(materials);

            string mainMaterialName = materials.FirstOrDefault() != null
                ? MaterialNameLocalizationService.ToSpanish(materials.First().Name)
                : null;

            Add(results, "Material principal",
                mainMaterialName,
                "Material Revit");

            Add(results, "Materiales",
                materialList,
                "Material Revit");

            Add(results, "Superficie o volumen del material",
                RevitExportDataBuilder.BuildAreaVolumeText(context, areaOrVolumeUnit),
                "Geometría / Material",
                areaOrVolumeUnit);

            Add(results, "Peso aproximado",
                FindFirst(context,
                    new[] { "Weight", "Peso", "Mass", "Masa", "Unit Weight", "Peso unitario" }),
                "Parámetro Revit");

            Add(results, "Número de instancias",
                context.Instances.Count.ToString(),
                "Revit");

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

        private static string FindFirst(RevitTypeContext context, IEnumerable<string> candidateNames)
        {
            string valueFromType = RevitParameterSearchService.FindFirstParameterValue(
                context.ElementType,
                candidateNames,
                context.Document);

            if (!string.IsNullOrWhiteSpace(valueFromType))
                return valueFromType;

            return RevitParameterSearchService.FindFirstParameterValue(
                context.Instances,
                candidateNames,
                context.Document);
        }

        private static string FindProductName(RevitTypeContext context)
        {
            if (context == null)
                return null;

            string fromType = RevitParameterSearchService.FindFirstParameterValue(
                context.ElementType,
                new[]
                {
                    "Description",
                    "Descripción",
                    "Model",
                    "Modelo",
                    "Product Name",
                    "Nombre del Producto",
                    "Nombre del producto",
                    "Product Code",
                    "Código de Producto",
                    "Codigo de Producto"
                },
                context.Document);

            if (RevitExportDataBuilder.IsReasonableText(fromType, false, 3))
                return fromType;

            string fromInstances = RevitParameterSearchService.FindFirstParameterValue(
                context.Instances,
                new[]
                {
                    "Description",
                    "Descripción",
                    "Model",
                    "Modelo",
                    "Product Name",
                    "Nombre del Producto",
                    "Nombre del producto",
                    "Product Code",
                    "Código de Producto",
                    "Codigo de Producto"
                },
                context.Document);

            if (RevitExportDataBuilder.IsReasonableText(fromInstances, false, 3))
                return fromInstances;

            string typeName = context.ElementType?.Name;
            if (RevitExportDataBuilder.IsReasonableText(typeName, false, 3))
                return typeName;

            return null;
        }

        private static string FindManufacturer(RevitTypeContext context)
        {
            string manufacturer = FindFirst(context, new[]
            {
                "Manufacturer",
                "Fabricante",
                "Marca"
            });

            return RevitExportDataBuilder.IsReasonableText(manufacturer, false)
                ? manufacturer
                : null;
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
