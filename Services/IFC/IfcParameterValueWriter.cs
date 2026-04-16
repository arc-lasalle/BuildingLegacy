using Autodesk.Revit.DB;
using BLComponentTemplate.Models;
using BLComponentTemplate.Services.Revit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BLComponentTemplate.Services.IFC
{
    public static class IfcParameterValueWriter
    {
        private static readonly HashSet<string> TypeParameterNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "BL_ProductName",
            "BL_Manufacturer",
            "BL_ModelReference",
            "BL_Color",
            "BL_Finish",
            "BL_MainMaterial",
            "BL_Materials",
            "BL_Weight",
            "BL_ThermalTransmittance",
            "BL_MaterialConductivities",
            "BL_MaterialDensities"
        };

        private static readonly HashSet<string> InstanceParameterNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "BL_BuildingLocation",
            "BL_Dimensions",
            "BL_MaterialAreaOrVolume",
            "BL_InstanceCount"
        };

        public static void Write(Document doc, ExportSessionContext context)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            RevitTypeContext typeContext = RevitTypeContextBuilder.Build(doc, context.SelectedMatch);
            if (typeContext == null)
                throw new InvalidOperationException("No se pudo construir el contexto del tipo para IFC.");

            Dictionary<string, string> values = BuildIfcParameterValues(context);

            using (Transaction tx = new Transaction(doc, "BL - Write IFC Parameters"))
            {
                tx.Start();

                WriteTypeParameters(typeContext.ElementType, values);
                WriteInstanceParameters(typeContext.Instances, values);

                tx.Commit();
            }
        }

        private static Dictionary<string, string> BuildIfcParameterValues(ExportSessionContext context)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (ExportableParameter p in context.ExportableParameters.Where(x => x.IsSelectedForExport))
            {
                switch (p.DisplayName)
                {
                    case "Nombre del producto":
                        map["BL_ProductName"] = p.Value;
                        break;

                    case "Marca o Fabricante":
                        map["BL_Manufacturer"] = p.Value;
                        break;

                    case "Nombre del modelo de producto o referencia":
                        map["BL_ModelReference"] = p.Value;
                        break;

                    case "Ubicación actual del edificio (dirección física)":
                        map["BL_BuildingLocation"] = p.Value;
                        break;

                    case "Medidas del componente":
                        map["BL_Dimensions"] = p.Value;
                        break;

                    case "Color del componente":
                        map["BL_Color"] = p.Value;
                        break;

                    case "Acabado del componente":
                        map["BL_Finish"] = p.Value;
                        break;

                    case "Material principal":
                        map["BL_MainMaterial"] = p.Value;
                        break;

                    case "Materiales":
                        map["BL_Materials"] = p.Value;
                        break;

                    case "Superficie o volumen del material":
                        map["BL_MaterialAreaOrVolume"] = p.Value;
                        break;

                    case "Peso aproximado":
                        map["BL_Weight"] = p.Value;
                        break;

                    case "Transmitancia térmica componente":
                        map["BL_ThermalTransmittance"] = p.Value;
                        break;

                    case "Conductividad térmica materiales":
                        map["BL_MaterialConductivities"] = p.Value;
                        break;

                    case "Densidad materiales":
                        map["BL_MaterialDensities"] = p.Value;
                        break;

                    case "Número de instancias":
                        map["BL_InstanceCount"] = p.Value;
                        break;
                }
            }

            return map;
        }

        private static void WriteTypeParameters(ElementType elementType, Dictionary<string, string> values)
        {
            if (elementType == null)
                return;

            foreach (var kvp in values)
            {
                if (!TypeParameterNames.Contains(kvp.Key))
                    continue;

                WriteParameterValue(elementType, kvp.Key, kvp.Value);
            }
        }

        private static void WriteInstanceParameters(IEnumerable<Element> instances, Dictionary<string, string> values)
        {
            if (instances == null)
                return;

            foreach (Element instance in instances)
            {
                foreach (var kvp in values)
                {
                    if (!InstanceParameterNames.Contains(kvp.Key))
                        continue;

                    WriteParameterValue(instance, kvp.Key, kvp.Value);
                }
            }
        }

        private static void WriteParameterValue(Element element, string parameterName, string value)
        {
            if (element == null || string.IsNullOrWhiteSpace(parameterName))
                return;

            Parameter parameter = element.LookupParameter(parameterName);
            if (parameter == null || parameter.IsReadOnly)
                return;

            if (parameter.StorageType == StorageType.Integer)
            {
                if (int.TryParse(value, out int intValue))
                {
                    parameter.Set(intValue);
                }
                else
                {
                    parameter.Set(0);
                }
            }
            else
            {
                parameter.Set(value ?? string.Empty);
            }
        }
    }
}
