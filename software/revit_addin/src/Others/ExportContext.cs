using Autodesk.Revit.DB;
using BLComponentTemplate.Models;

namespace BLComponentTemplate.Others
{
    public class ExportContext
    {
        public Document Document { get; set; }
        public string ComponentType { get; set; }
        public ComponentMatch SelectedMatch { get; set; }
        public string TemplatePath { get; set; }
        public string OutputPath { get; set; }
    }
}