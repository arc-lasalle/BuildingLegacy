using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.DB;

namespace BLComponentTemplate.Services.Export.Extractors
{
    public static class MetalStructureDataExtractorHelper
    {
        public static string BuildEnvelopeDimensionsText(List<Element> elements, string targetUnit)
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

        public static string BuildEnvelopeVolumeText(List<Element> elements, string targetUnit)
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
    }
}