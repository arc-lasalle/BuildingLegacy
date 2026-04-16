using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace BLComponentTemplate.Models
{
    public class RevitTypeContext
    {
        public Document Document { get; set; }

        public ElementType ElementType { get; set; }

        public List<Element> Instances { get; set; } = new List<Element>();

        public string TypeId { get; set; }
    }
}