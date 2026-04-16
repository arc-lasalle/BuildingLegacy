using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace BLComponentTemplate
{
    public static class RevitParameterSearchService
    {
        public static Parameter FindFirstParameter(Element element, IEnumerable<string> candidateNames)
        {
            if (element == null || candidateNames == null)
                return null;

            foreach (string candidate in candidateNames)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                Parameter p = element.LookupParameter(candidate);
                if (p != null)
                    return p;
            }

            return null;
        }

        public static Parameter FindFirstParameter(IEnumerable<Element> elements, IEnumerable<string> candidateNames)
        {
            if (elements == null)
                return null;

            foreach (Element element in elements)
            {
                Parameter p = FindFirstParameter(element, candidateNames);
                if (p != null)
                    return p;
            }

            return null;
        }       

        public static string FindFirstParameterValue(Element element, IEnumerable<string> candidateNames, Document doc)
        {
            if (element == null || candidateNames == null)
                return null;

            foreach (string candidate in candidateNames)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                Parameter p = element.LookupParameter(candidate);
                if (p != null && p.HasValue)
                {
                    string value = GetParameterValueAsString(p, doc);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }

            return null;
        }

        public static string FindFirstParameterValue(IEnumerable<Element> elements, IEnumerable<string> candidateNames, Document doc)
        {
            if (elements == null)
                return null;

            foreach (Element element in elements)
            {
                string value = FindFirstParameterValue(element, candidateNames, doc);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static string GetParameterValueAsString(Parameter p, Document doc)
        {
            if (p == null || !p.HasValue)
                return null;

            try
            {
                switch (p.StorageType)
                {
                    case StorageType.String:
                        return p.AsString();

                    case StorageType.Integer:
                        return p.AsInteger().ToString();

                    case StorageType.Double:
                        return p.AsValueString();

                    case StorageType.ElementId:
                        ElementId id = p.AsElementId();
                        Element referenced = doc.GetElement(id);
                        return referenced != null ? referenced.Name : id.Value.ToString();

                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}