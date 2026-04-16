using Autodesk.Revit.DB;
using BLComponentTemplate.Models;
using System.Collections.Generic;

namespace BLComponentTemplate.Services.Export.Extractors
{
    public interface IComponentDataExtractor
    {
        List<ExportableParameter> Extract(
            Document doc,
            RevitTypeContext context,
            string componentType,
            ComponentMatch selectedMatch,
            List<ComponentMatch> selectedMatches,
            string dimensionsUnit,
            string areaOrVolumeUnit);
    }
}
