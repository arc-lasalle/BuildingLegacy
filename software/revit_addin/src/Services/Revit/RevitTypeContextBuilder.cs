using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using BLComponentTemplate.Models;

namespace BLComponentTemplate.Services.Revit
{
    public static class RevitTypeContextBuilder
    {
        public static RevitTypeContext Build(Document doc, ComponentMatch selectedMatch)
        {
            if (doc == null || selectedMatch == null)
                return null;

            if (!int.TryParse(selectedMatch.ElementId, out int typeIdInt))
                return null;

            ElementId typeId = new ElementId(typeIdInt);
            ElementType elementType = doc.GetElement(typeId) as ElementType;

            if (elementType == null)
                return null;

            List<Element> instances = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e.GetTypeId() == typeId)
                .ToList();

            return new RevitTypeContext
            {
                Document = doc,
                ElementType = elementType,
                Instances = instances,
                TypeId = selectedMatch.ElementId
            };
        }
    }
}