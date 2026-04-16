using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace BLComponentTemplate.Services.Rvt
{
    public static class RvtAssemblyExportService
    {
        public static void Export(Document sourceDoc, ExportSessionContext context, string outputFilePath)
        {
            if (sourceDoc == null)
                throw new ArgumentNullException(nameof(sourceDoc));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("La ruta de salida RVT no es válida.", nameof(outputFilePath));

            if (context.SelectedMatches == null || context.SelectedMatches.Count == 0)
                throw new InvalidOperationException("No hay subcomponentes seleccionados para exportar.");

            string folder = Path.GetDirectoryName(outputFilePath);
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                throw new DirectoryNotFoundException("La carpeta de salida no existe.");

            List<ElementId> sourceElementIds = context.SelectedMatches
                .SelectMany(m => m.InstanceElementIds ?? new List<int>())
                .Distinct()
                .Select(id => new ElementId(id))
                .Where(id => sourceDoc.GetElement(id) != null)
                .ToList();

            if (sourceElementIds.Count == 0)
                throw new InvalidOperationException("No se encontraron elementos válidos para copiar al nuevo proyecto.");

            Application app = sourceDoc.Application;
            Document targetDoc = app.NewProjectDocument(UnitSystem.Metric);

            if (targetDoc == null)
                throw new InvalidOperationException("No se pudo crear el nuevo proyecto RVT.");

            try
            {
                List<ElementId> copiedIds;

                using (Transaction tx = new Transaction(targetDoc, "BL - Copy assembly elements"))
                {
                    tx.Start();

                    CopyPasteOptions options = new CopyPasteOptions();

                    copiedIds = ElementTransformUtils.CopyElements(
                        sourceDoc,
                        sourceElementIds,
                        targetDoc,
                        Transform.Identity,
                        options).ToList();

                    if (copiedIds.Count == 0)
                        throw new InvalidOperationException("La copia de elementos al nuevo proyecto no devolvió ningún elemento.");

                    CreatePreview3DView(targetDoc, copiedIds);

                    tx.Commit();
                }

                SaveAsOptions saveOptions = new SaveAsOptions
                {
                    OverwriteExistingFile = true
                };

                targetDoc.SaveAs(outputFilePath, saveOptions);
            }
            finally
            {
                if (targetDoc != null && targetDoc.IsValidObject)
                {
                    targetDoc.Close(false);
                }
            }
        }

        private static void CreatePreview3DView(Document doc, List<ElementId> copiedIds)
        {
            ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .First(v => v.ViewFamily == ViewFamily.ThreeDimensional);

            View3D view = View3D.CreateIsometric(doc, viewFamilyType.Id);
            view.Name = "BL_Assembly_3D";

            BoundingBoxXYZ bbox = BuildBoundingBox(doc, copiedIds);
            if (bbox != null)
            {
                view.IsSectionBoxActive = true;
                view.SetSectionBox(ExpandBoundingBox(bbox, 0.10));
            }
        }

        private static BoundingBoxXYZ BuildBoundingBox(Document doc, List<ElementId> elementIds)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;
            bool any = false;

            foreach (ElementId id in elementIds)
            {
                Element e = doc.GetElement(id);
                if (e == null)
                    continue;

                BoundingBoxXYZ bb = e.get_BoundingBox(null);
                if (bb == null)
                    continue;

                any = true;

                minX = Math.Min(minX, bb.Min.X);
                minY = Math.Min(minY, bb.Min.Y);
                minZ = Math.Min(minZ, bb.Min.Z);

                maxX = Math.Max(maxX, bb.Max.X);
                maxY = Math.Max(maxY, bb.Max.Y);
                maxZ = Math.Max(maxZ, bb.Max.Z);
            }

            if (!any)
                return null;

            return new BoundingBoxXYZ
            {
                Min = new XYZ(minX, minY, minZ),
                Max = new XYZ(maxX, maxY, maxZ)
            };
        }

        private static BoundingBoxXYZ ExpandBoundingBox(BoundingBoxXYZ bbox, double marginFactor)
        {
            XYZ min = bbox.Min;
            XYZ max = bbox.Max;

            double dx = (max.X - min.X) * marginFactor;
            double dy = (max.Y - min.Y) * marginFactor;
            double dz = (max.Z - min.Z) * marginFactor;

            // margen mínimo para evitar cajas degeneradas
            dx = Math.Max(dx, 0.5);
            dy = Math.Max(dy, 0.5);
            dz = Math.Max(dz, 0.5);

            return new BoundingBoxXYZ
            {
                Min = new XYZ(min.X - dx, min.Y - dy, min.Z - dz),
                Max = new XYZ(max.X + dx, max.Y + dy, max.Z + dz)
            };
        }
    }
}