using System;
using System.IO;
using System.Text;

namespace BLComponentTemplate.Services.IFC
{
    public static class IfcUserDefinedPsetFileService
    {
        public static string CreateFile()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                $"BL_IFC_Pset_{Guid.NewGuid():N}.txt");

            string content = BuildContent();
            File.WriteAllText(path, content, Encoding.UTF8);

            return path;
        }

        private static string BuildContent()
        {
            var sb = new StringBuilder();

            // ---------------------------------------------------------
            // INSTANCE PROPERTIES
            // IFC4: IfcDoor
            // ---------------------------------------------------------
            sb.AppendLine("PropertySet:\tBLComponentTemplate_Instance\tI\tIfcDoor");
            sb.AppendLine("BuildingLocation\tText\tBL_BuildingLocation");
            sb.AppendLine("Dimensions\tText\tBL_Dimensions");
            sb.AppendLine("MaterialAreaOrVolume\tText\tBL_MaterialAreaOrVolume");
            sb.AppendLine("InstanceCount\tInteger\tBL_InstanceCount");
            sb.AppendLine();

            // ---------------------------------------------------------
            // TYPE PROPERTIES
            // IFC4: IfcDoorType
            // ---------------------------------------------------------
            sb.AppendLine("PropertySet:\tBLComponentTemplate_Instance\tT\tIfcDoorType");
            sb.AppendLine("ProductName\tText\tBL_ProductName");
            sb.AppendLine("Manufacturer\tText\tBL_Manufacturer");
            sb.AppendLine("ModelReference\tText\tBL_ModelReference");
            sb.AppendLine("Color\tText\tBL_Color");
            sb.AppendLine("Finish\tText\tBL_Finish");
            sb.AppendLine("MainMaterial\tText\tBL_MainMaterial");
            sb.AppendLine("Materials\tText\tBL_Materials");
            sb.AppendLine("Weight\tText\tBL_Weight");
            sb.AppendLine("ThermalTransmittance\tText\tBL_ThermalTransmittance");
            sb.AppendLine("MaterialConductivities\tText\tBL_MaterialConductivities");
            sb.AppendLine("MaterialDensities\tText\tBL_MaterialDensities");

            return sb.ToString();
        }
    }
}
