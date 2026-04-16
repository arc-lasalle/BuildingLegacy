using System.Collections.Generic;
using Autodesk.Revit.DB;
using BLComponentTemplate.Others;

namespace BLComponentTemplate.Utils
{
    public static class ComponentSearchRuleResolver
    {
        public static ComponentSearchRule GetRule(string componentType)
        {
            switch (componentType)
            {
                case "Puertas cortafuegos":
                    return new ComponentSearchRule
                    {
                        Categories = new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_Doors
                        },
                        FamilyNameContainsAny = new List<string>
                        {
                            "fire", "cortafuego", "rf", "ei"
                        },
                        TypeNameContainsAny = new List<string>
                        {
                            "fire", "cortafuego", "rf", "ei"
                        },
                        ParameterNameContainsAny = new List<string>
                        {
                            "fire rating", "resistance", "rating"
                        },
                        ParameterValueContainsAny = new List<string>
                        {
                            "ei", "rf", "60", "90", "120"
                        }
                    };

                case "Puerta enrollable":
                    return new ComponentSearchRule
                    {
                        Categories = new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_Doors
                        },
                        FamilyNameContainsAny = new List<string>
                        {
                            "rollable", "rolling", "enrollable", "shutter", "persiana"
                        },
                        TypeNameContainsAny = new List<string>
                        {
                            "rollable", "rolling", "enrollable", "shutter", "persiana"
                        }
                    };

                case "Vidrios":
                    return new ComponentSearchRule
                    {
                        Categories = new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_CurtainWallPanels
                        },
                        ElementNameContainsAny = new List<string>
                        {
                            "glass",
                            "glazing",
                            "vidrio",
                            "cristal",
                            "panel"
                        },
                        CategoryNameContainsAny = new List<string>
                        {
                            "panel"
                        }
                    };

                case "Pilares de acero":
                    return new ComponentSearchRule
                    {
                        Categories = new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_StructuralColumns
                        },
                        FamilyNameContainsAny = new List<string>
                        {
                            "steel",
                            "metal",
                            "acero",
                            "heb",
                            "hea",
                            "ipe",
                            "upn",
                            "column",
                            "columna"
                        },
                        TypeNameContainsAny = new List<string>
                        {
                            "steel",
                            "metal",
                            "acero",
                            "heb",
                            "hea",
                            "ipe",
                            "upn",
                            "column",
                            "columna"
                        },
                        ElementNameContainsAny = new List<string>
                        {
                            "steel",
                            "metal",
                            "acero",
                            "column",
                            "columna",
                            "heb",
                            "hea",
                            "ipe",
                            "upn"
                        },
                        CategoryNameContainsAny = new List<string>
                        {
                            "structural columns",
                            "columnas estructurales",
                            "column",
                            "columna"
                        }
                    };

                case "Estructuras metálicas":
                    return new ComponentSearchRule
                    {
                        Categories = new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_StructuralColumns,
                            BuiltInCategory.OST_StructuralFraming
                        },
                        FamilyNameContainsAny = new List<string>
                        {
                            "steel", "metal", "acero", "metallic"
                        },
                        TypeNameContainsAny = new List<string>
                        {
                            "steel", "metal", "acero", "metallic", "ipe", "heb", "hea", "upn"
                        }
                    };

                case "Bombas de calor":
                    return new ComponentSearchRule
                    {
                        Categories = new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_MechanicalEquipment
                        },
                        FamilyNameContainsAny = new List<string>
                        {
                            "heat pump",
                            "bomba de calor",
                            "pump",
                            "hvac",
                            "air source",
                            "aerotermia"
                        },
                        TypeNameContainsAny = new List<string>
                        {
                            "heat pump",
                            "bomba de calor",
                            "pump",
                            "hvac",
                            "air source",
                            "aerotermia"
                        },
                        ElementNameContainsAny = new List<string>
                        {
                            "heat pump",
                            "bomba de calor",
                            "pump",
                            "hvac",
                            "air source",
                            "aerotermia"
                        },
                        CategoryNameContainsAny = new List<string>
                        {
                            "mechanical equipment",
                            "equipos mecánicos",
                            "equipment",
                            "equipo"
                        }
                    };

                case "Sistemas de climatización":
                    return new ComponentSearchRule
                    {
                        Categories = new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_MechanicalEquipment,
                            BuiltInCategory.OST_DuctTerminal,
                            BuiltInCategory.OST_DuctAccessory,
                            BuiltInCategory.OST_DuctFitting,
                            BuiltInCategory.OST_DuctCurves
                        },
                        FamilyNameContainsAny = new List<string>
                        {
                            "hvac", "climat", "air", "duct", "vent", "fan", "ahu", "uta"
                        },
                        TypeNameContainsAny = new List<string>
                        {
                            "hvac", "climat", "air", "duct", "vent", "fan", "ahu", "uta"
                        }
                    };

                case "Lonas":
                    return new ComponentSearchRule
                    {
                        Categories = new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_GenericModel,
                            BuiltInCategory.OST_Roofs
                        },
                        Keywords = new List<string>
                        {
                            "lona",
                            "tarpaulin",
                            "canvas",
                            "fabric",
                            "toldo",
                            "cubierta",
                            "cover",
                            "roof",
                            "membrana",
                            "membrane"
                        }
                    };

                case "Ventanas":
                    return new ComponentSearchRule
                    {
                        Categories = new List<BuiltInCategory>
                        {
                            BuiltInCategory.OST_Windows
                        }
                    };

                default:
                    return new ComponentSearchRule();
            }
        }
    }
}