using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace BLComponentTemplate
{
    public class ExportSessionContext
    {
        public Document Document { get; set; }

        public string ComponentType { get; set; }

        public bool PreciseMode { get; set; }

        public ComponentMatch SelectedMatch { get; set; }

        public List<ComponentMatch> SelectedMatches { get; set; } = new List<ComponentMatch>();

        public string ImportedTemplatePath { get; set; }

        public string ComponentDimensionsUnit { get; set; } = "mm";

        public string MaterialAreaOrVolumeUnit { get; set; } = "m3";

        public string ElementScale { get; set; } 

        public List<ExportableParameter> ExportableParameters { get; set; } = new List<ExportableParameter>();

        //public bool CompositeExportMode { get; set; } = false;
    }
}