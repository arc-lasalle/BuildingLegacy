using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace BLComponentTemplate.Others
{
    public class ComponentSearchRule
    {
        public List<BuiltInCategory> Categories { get; set; } = new List<BuiltInCategory>();

        public List<string> FamilyNameContainsAny { get; set; } = new List<string>();

        public List<string> TypeNameContainsAny { get; set; } = new List<string>();

        public List<string> ParameterNameContainsAny { get; set; } = new List<string>();

        public List<string> ParameterValueContainsAny { get; set; } = new List<string>();

        public List<string> Keywords { get; set; } = new List<string>();

        public List<string> ElementNameContainsAny { get; set; }

        public List<string> CategoryNameContainsAny { get; set; }

        public List<string> ClassNameContainsAny { get; set; }
    }
}