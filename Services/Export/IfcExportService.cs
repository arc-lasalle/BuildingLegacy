using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;

namespace BLComponentTemplate.Services.Export
{
    public static class IfcExportService
    {
        public static void Export(Document doc, string outputFilePath, ExportSessionContext context)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("La ruta de salida IFC no es válida.", nameof(outputFilePath));

            string folder = Path.GetDirectoryName(outputFilePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputFilePath);

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                throw new DirectoryNotFoundException("La carpeta de salida del IFC no existe.");

            View3D exportView = null;

            try
            {
                using (Transaction txCreateView = new Transaction(doc, "BL IFC - Crear vista temporal"))
                {
                    txCreateView.Start();

                    exportView = CreateTemporary3DView(doc);

                    List<ElementId> elementIds = GetElementsForExport(doc, context);

                    if (elementIds.Count == 0)
                        throw new InvalidOperationException("No se encontraron elementos para exportar.");

                    PrepareViewForExport(doc, exportView, elementIds);

                    txCreateView.Commit();
                }

                using (Transaction txExport = new Transaction(doc, "BL IFC - Exportar"))
                {
                    txExport.Start();

                    string userDefinedPsetFile = BLComponentTemplate.Services.IFC.IfcUserDefinedPsetFileService.CreateFile();

                    IFCExportOptions options = new IFCExportOptions
                    {
                        FileVersion = IFCVersion.IFC4,
                        ExportBaseQuantities = true,
                        FilterViewId = exportView.Id
                    };

                    options.AddOption("ExportIFCCommonPropertySets", "true");
                    options.AddOption("ExportRevitPropertySets", "true");
                    options.AddOption("VisibleElementsOfCurrentView", "true");
                    options.AddOption("UseActiveViewGeometry", "true");
                    options.AddOption("ActiveViewId", exportView.Id.Value.ToString());
                    options.AddOption("ExportUserDefinedPsets", "true");
                    options.AddOption("ExportUserDefinedPsetsFileName", userDefinedPsetFile);

                    bool success = doc.Export(folder, fileNameWithoutExtension, options);

                    if (!success)
                    {
                        txExport.RollBack();
                        throw new InvalidOperationException("La exportación IFC no se completó correctamente.");
                    }

                    txExport.Commit();
                }

                using (Transaction txDeleteView = new Transaction(doc, "BL IFC - Borrar vista temporal"))
                {
                    txDeleteView.Start();

                    if (exportView != null && exportView.IsValidObject)
                    {
                        doc.Delete(exportView.Id);
                    }

                    txDeleteView.Commit();
                }

                string expectedPath = Path.Combine(folder, fileNameWithoutExtension + ".ifc");

                if (!File.Exists(expectedPath) && !File.Exists(outputFilePath))
                {
                    throw new InvalidOperationException(
                        "Revit indicó que la exportación se completó, pero no se encontró el fichero IFC en la ruta esperada.");
                }
            }
            catch
            {
                if (exportView != null && exportView.IsValidObject)
                {
                    try
                    {
                        using (Transaction txCleanup = new Transaction(doc, "BL IFC - Limpieza"))
                        {
                            txCleanup.Start();
                            doc.Delete(exportView.Id);
                            txCleanup.Commit();
                        }
                    }
                    catch
                    {
                    }
                }

                throw;
            }
        }

        private static List<ElementId> GetElementsForExport(Document doc, ExportSessionContext context)
        {
            bool isSystemAssembly =
                string.Equals(context.ElementScale, "Sistema", StringComparison.OrdinalIgnoreCase)
                && context.SelectedMatches != null
                && context.SelectedMatches.Count > 0;

            if (isSystemAssembly)
            {
                return context.SelectedMatches
                    .SelectMany(m => m.InstanceElementIds ?? new List<int>())
                    .Distinct()
                    .Select(id => new ElementId(id))
                    .Where(id => doc.GetElement(id) != null)
                    .ToList();
            }

            return GetElementsOfSelectedType(doc, context.SelectedMatch);
        }

        private static View3D CreateTemporary3DView(Document doc)
        {
            ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .First(v => v.ViewFamily == ViewFamily.ThreeDimensional);

            View3D view = View3D.CreateIsometric(doc, viewFamilyType.Id);
            view.Name = $"BL_IFC_Export_Temp_{Guid.NewGuid():N}".Substring(0, 20);

            return view;
        }

        private static void PrepareViewForExport(Document doc, View3D view, List<ElementId> elementIds)
        {
            List<ElementId> allViewElements = new FilteredElementCollector(doc, view.Id)
                .WhereElementIsNotElementType()
                .ToElementIds()
                .ToList();

            HashSet<long> selectedIds = elementIds
               .Select(id => id.Value)
               .ToHashSet();

            List<ElementId> toHide = allViewElements
                .Where(id => !selectedIds.Contains(id.Value))
                .Where(id =>
                {
                    Element e = doc.GetElement(id);
                    if (e == null)
                        return false;

                    if (e.IsHidden(view))
                        return false;

                    return e.CanBeHidden(view);
                })
                .ToList();

            if (toHide.Count > 0)
            {
                view.HideElements(toHide);
            }
        }

        private static List<ElementId> GetElementsOfSelectedType(Document doc, ComponentMatch match)
        {
            if (match == null)
                return new List<ElementId>();

            var elements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e.Category != null)
                .Where(e =>
                {
                    if (e is FamilyInstance fi)
                    {
                        var type = fi.Symbol;
                        if (type == null)
                            return false;

                        bool sameType =
                            type.Family.Name == match.Family &&
                            type.Name == match.TypeName;

                        if (!sameType)
                            return false;

                        string signature = BuildDimensionsSignature(fi, type, doc);

                        return signature == match.DimensionsSignature;
                    }

                    return false;
                })
                .ToList();

            Element first = elements.FirstOrDefault();

            if (first == null)
                return new List<ElementId>();

            return new List<ElementId> { first.Id };
        }

        private static string BuildDimensionsSignature(Element instance, ElementType elementType, Document doc)
        {
            string width = FindDimensionValue(instance, elementType, doc,
                new[] { "Width", "Anchura", "Ancho", "Frame Width", "Rough Width" });

            string height = FindDimensionValue(instance, elementType, doc,
                new[] { "Height", "Altura", "Frame Height", "Rough Height" });

            string thickness = FindDimensionValue(instance, elementType, doc,
                new[] { "Thickness", "Espesor", "Depth", "Profundidad" });

            List<string> parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(width))
                parts.Add($"W={width}");

            if (!string.IsNullOrWhiteSpace(height))
                parts.Add($"H={height}");

            if (!string.IsNullOrWhiteSpace(thickness))
                parts.Add($"T={thickness}");

            return parts.Count == 0 ? "(sin dimensiones)" : string.Join(", ", parts);
        }

        private static string FindDimensionValue(
            Element instance,
            ElementType elementType,
            Document doc,
            IEnumerable<string> candidateNames)
        {
            string fromInstance = RevitParameterSearchService.FindFirstParameterValue(instance, candidateNames, doc);
            if (!string.IsNullOrWhiteSpace(fromInstance))
                return fromInstance;

            string fromType = RevitParameterSearchService.FindFirstParameterValue(elementType, candidateNames, doc);
            if (!string.IsNullOrWhiteSpace(fromType))
                return fromType;

            return null;
        }
    }
}