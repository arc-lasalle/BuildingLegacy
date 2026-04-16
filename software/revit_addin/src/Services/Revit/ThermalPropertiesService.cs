using Autodesk.Revit.DB;
using BLComponentTemplate.Models;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace BLComponentTemplate.Services.Revit
{
    public static class ThermalPropertiesService
    {
        public static string GetFamilyUValue(ElementType elementType)
        {
            if (elementType is not FamilySymbol familySymbol)
                return null;

            try
            {
                FamilyThermalProperties thermalProps = familySymbol.GetThermalProperties();
                if (thermalProps == null)
                    return null;

                double uValue = thermalProps.HeatTransferCoefficient;
                if (uValue <= 0)
                    return null;

                return uValue.ToString("0.###", CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        public static List<Material> GetUniqueMaterialsFromContext(RevitTypeContext context)
        {
            if (context?.Instances == null || context.Document == null)
                return new List<Material>();

            return context.Instances
                .SelectMany(i => MaterialExtractionService.GetMaterials(i, context.Document))
                .GroupBy(m => m.Id.Value)
                .Select(g => g.First())
                .OrderBy(m => m.Id.Value)
                .ToList();
        }

        public static string GetMaterialsConductivity(RevitTypeContext context)
        {
            List<Material> materials = GetUniqueMaterialsFromContext(context);
            if (materials.Count == 0 || context?.Document == null)
                return null;

            var values = new List<string>();

            foreach (Material material in materials)
            {
                ThermalAsset asset = GetThermalAsset(material, context.Document);
                if (asset == null)
                    continue;

                if (asset.ThermalMaterialType != ThermalMaterialType.Solid)
                    continue;

                if (asset.ThermalConductivity <= 0)
                    continue;

                double conductivity = UnitUtils.ConvertFromInternalUnits(
                    asset.ThermalConductivity,
                    UnitTypeId.WattsPerMeterKelvin);
             
                values.Add(conductivity.ToString(CultureInfo.InvariantCulture));
            }

            return values.Count == 0 ? null : string.Join("; ", values);
        }

        public static string GetMaterialsDensity(RevitTypeContext context)
        {
            List<Material> materials = GetUniqueMaterialsFromContext(context);
            if (materials.Count == 0 || context?.Document == null)
                return null;

            var values = new List<string>();

            foreach (Material material in materials)
            {
                ThermalAsset asset = GetThermalAsset(material, context.Document);
                if (asset == null)
                    continue;

                if (asset.ThermalMaterialType != ThermalMaterialType.Solid)
                    continue;

                if (asset.Density <= 0)
                    continue;

                double density = UnitUtils.ConvertFromInternalUnits(
                    asset.Density,
                    UnitTypeId.KilogramsPerCubicMeter);
              
                values.Add(density.ToString(CultureInfo.InvariantCulture));
            }

            return values.Count == 0 ? null : string.Join("; ", values);
        }      

        private static ThermalAsset GetThermalAsset(Material material, Document doc)
        {
            if (material == null || doc == null)
                return null;

            ElementId thermalAssetId = material.ThermalAssetId;
            if (thermalAssetId == ElementId.InvalidElementId)
                return null;

            PropertySetElement propertySet = doc.GetElement(thermalAssetId) as PropertySetElement;
            if (propertySet == null)
                return null;

            try
            {
                return propertySet.GetThermalAsset();
            }
            catch
            {
                return null;
            }
        }
    }
}
