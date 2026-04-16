using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace BLComponentTemplate.Utils
{
    public static class ComponentCategoryResolver
    {
        public static List<BuiltInCategory> GetCategories(string componentType)
        {
            switch (componentType)
            {
                case "Puertas cortafuegos":
                    return new List<BuiltInCategory>
                    {
                        BuiltInCategory.OST_Doors
                    };

                case "Puerta enrollable":
                    return new List<BuiltInCategory>
                    {
                        BuiltInCategory.OST_Doors
                    };

                case "Ventanas":
                    return new List<BuiltInCategory>
                    {
                        BuiltInCategory.OST_Windows
                    };

                case "Estructuras metálicas":
                    return new List<BuiltInCategory>
                    {
                        BuiltInCategory.OST_StructuralColumns,
                        BuiltInCategory.OST_StructuralFraming
                    };

                case "Sistemas de climatización":
                    return new List<BuiltInCategory>
                    {
                        BuiltInCategory.OST_MechanicalEquipment,
                        BuiltInCategory.OST_DuctTerminal,
                        BuiltInCategory.OST_DuctAccessory,
                        BuiltInCategory.OST_DuctFitting,
                        BuiltInCategory.OST_DuctCurves
                    };

                case "Lonas":
                    return new List<BuiltInCategory>
                    {
                        BuiltInCategory.OST_GenericModel
                    };

                default:
                    return new List<BuiltInCategory>();
            }
        }
    }
}
