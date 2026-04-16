using System;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;

namespace BLComponentTemplate.Services.IFC
{
    public static class IfcSharedParameterService
    {
        private const string GROUP_NAME = "BL_ComponentTemplate";

        public static void EnsureParameters(Document doc)
        {
            Application app = doc.Application;

            string tempFile = Path.Combine(Path.GetTempPath(), "BL_SharedParameters.txt");

            if (!File.Exists(tempFile))
            {
                File.WriteAllText(tempFile, "");
            }

            app.SharedParametersFilename = tempFile;

            DefinitionFile defFile = app.OpenSharedParameterFile();
            if (defFile == null)
                throw new InvalidOperationException("No se pudo abrir el fichero de shared parameters.");

            DefinitionGroup group = defFile.Groups.get_Item(GROUP_NAME)
                ?? defFile.Groups.Create(GROUP_NAME);

            using (Transaction tx = new Transaction(doc, "BL - Create Shared Parameters"))
            {
                tx.Start();

                CreateAndBind(doc, group, "BL_ProductName", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_Manufacturer", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_ModelReference", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_BuildingLocation", SpecTypeId.String.Text, false);
                CreateAndBind(doc, group, "BL_Dimensions", SpecTypeId.String.Text, false);
                CreateAndBind(doc, group, "BL_Color", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_Finish", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_MainMaterial", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_Materials", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_MaterialAreaOrVolume", SpecTypeId.String.Text, false);
                CreateAndBind(doc, group, "BL_Weight", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_ThermalTransmittance", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_MaterialConductivities", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_MaterialDensities", SpecTypeId.String.Text, true);
                CreateAndBind(doc, group, "BL_InstanceCount", SpecTypeId.Int.Integer, false);

                tx.Commit();
            }
        }

        private static void CreateAndBind(
            Document doc,
            DefinitionGroup group,
            string paramName,
            ForgeTypeId dataType,
            bool isType)
        {
            Definition definition = group.Definitions.get_Item(paramName);

            if (definition == null)
            {
                ExternalDefinitionCreationOptions options =
                    new ExternalDefinitionCreationOptions(paramName, dataType);

                definition = group.Definitions.Create(options);
            }

            CategorySet catSet = doc.Application.Create.NewCategorySet();
            catSet.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Doors));

            Binding binding = isType
                ? doc.Application.Create.NewTypeBinding(catSet)
                : doc.Application.Create.NewInstanceBinding(catSet);

            BindingMap map = doc.ParameterBindings;

            if (!map.Contains(definition))
            {
                map.Insert(definition, binding, GroupTypeId.Data);
            }
        }
    }
}