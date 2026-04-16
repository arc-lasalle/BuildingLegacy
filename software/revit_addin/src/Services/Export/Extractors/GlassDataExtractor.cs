using Autodesk.Revit.DB;
using BLComponentTemplate.Models;
using BLComponentTemplate.Services.Revit;
using BLComponentTemplate.Utils;
using System.Collections.Generic;
using System.Linq;

namespace BLComponentTemplate.Services.Export.Extractors
{
    public class GlassDataExtractor : IComponentDataExtractor
    {
        public List<ExportableParameter> Extract(
            Document doc,
            RevitTypeContext context,
            string componentType,
            ComponentMatch selectedMatch,
            List<ComponentMatch> selectedMatches,
            string dimensionsUnit,
            string areaOrVolumeUnit)
        {
            var results = new List<ExportableParameter>();

            if (doc == null || context?.Instances == null || context.Instances.Count == 0)
                return results;

            var elements = context.Instances;

            // --- Nombre ---
            Add(results, "Nombre del producto",
                context.ElementType?.Name,
                "Tipo Revit");

            // --- Ubicación ---
            Add(results, "Ubicación actual del edificio (dirección física)",
                FindBuildingAddress(doc),
                "Proyecto");

            // --- Dimensiones (ENVOLVENTE, no reales) ---
            Add(results, "Dimensiones aproximadas",
                BuildBoundingBoxDimensions(elements, dimensionsUnit),
                "BoundingBox",
                dimensionsUnit);

            // --- Área ---
            Add(results, "Superficie",
                BuildArea(elements, areaOrVolumeUnit),
                "Geometría",
                areaOrVolumeUnit);

            // --- Forma ---
            Add(results, "Forma",
                DetectShape(elements),
                "Inferida");

            // --- Material principal ---
            var materials = MaterialExtractionService.GetMaterials(elements, doc);

            if (materials.Any())
            {
                Add(results, "Material principal",
                    MaterialNameLocalizationService.ToSpanish(materials.First().Name),
                    "Material Revit");

                Add(results, "Materiales",
                    MaterialExtractionService.GetMaterialNamesAsText(materials),
                    "Material Revit");
            }

            // --- Número de instancias ---
            Add(results, "Número de instancias",
                elements.Count.ToString(),
                "Instancias");

            return results.Where(r => !string.IsNullOrWhiteSpace(r.Value)).ToList();
        }

        private static void Add(List<ExportableParameter> results, string name, string value, string source, string unit = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            results.Add(new ExportableParameter
            {
                DisplayName = name,
                Value = value,
                Source = source,
                Unit = unit,
                IsSelectedForExport = true
            });
        }

        // --- Bounding Box ---
        private static string BuildBoundingBoxDimensions(List<Element> elements, string unit)
        {
            var bb = GetBoundingBox(elements);
            if (bb == null) return null;

            double w = bb.Max.X - bb.Min.X;
            double h = bb.Max.Z - bb.Min.Z;

            return $"W={ConvertLength(w, unit)}, H={ConvertLength(h, unit)}";
        }

        private static string BuildArea(List<Element> elements, string unit)
        {
            var values = new HashSet<string>();

            foreach (var e in elements)
            {
                var p = RevitParameterSearchService.FindFirstParameter(
                    e,
                    new[] { "Area", "Host Area Computed", "Superficie" });

                if (p != null)
                {
                    string v = ConvertArea(p, unit);
                    if (!string.IsNullOrWhiteSpace(v))
                        values.Add(v);
                }
            }

            return values.Count == 0 ? null : string.Join("; ", values);
        }

        private static string DetectShape(List<Element> elements)
        {
            foreach (var element in elements)
            {
                var face = GetMainPlanarFace(element);
                if (face == null)
                    continue;

                var vertices = GetFaceVertices(face);

                if (vertices == null || vertices.Count < 3)
                    continue;

                int n = vertices.Count;

                if (n == 3)
                    return "Triangular";

                if (n == 4)
                {
                    if (IsRectangle(vertices))
                        return "Rectangular";

                    if (IsTrapezoid(vertices))
                        return "Trapezoidal";

                    return "Cuadrilátero irregular";
                }

                return "Poligonal";
            }

            return "Desconocida";
        }

        private static bool IsRectangle(List<XYZ> pts)
        {
            if (pts.Count != 4)
                return false;

            var vectors = new List<XYZ>();

            for (int i = 0; i < 4; i++)
            {
                var v = (pts[(i + 1) % 4] - pts[i]).Normalize();
                vectors.Add(v);
            }

            // comprobar perpendicularidad
            for (int i = 0; i < 4; i++)
            {
                double dot = vectors[i].DotProduct(vectors[(i + 1) % 4]);
                if (Math.Abs(dot) > 0.01)
                    return false;
            }

            return true;
        }

        private static bool IsTrapezoid(List<XYZ> pts)
        {
            if (pts.Count != 4)
                return false;

            var v1 = (pts[1] - pts[0]).Normalize();
            var v2 = (pts[2] - pts[3]).Normalize();

            var v3 = (pts[2] - pts[1]).Normalize();
            var v4 = (pts[3] - pts[0]).Normalize();

            bool pair1 = Math.Abs(Math.Abs(v1.DotProduct(v2)) - 1) < 0.01;
            bool pair2 = Math.Abs(Math.Abs(v3.DotProduct(v4)) - 1) < 0.01;

            return pair1 || pair2;
        }

        private static List<XYZ> GetFaceVertices(PlanarFace face)
        {
            var loops = face.GetEdgesAsCurveLoops();
            if (loops == null || loops.Count == 0)
                return null;

            CurveLoop outerLoop = loops
                .OrderByDescending(loop => loop.Sum(c => c.Length))
                .First();

            var vertices = new List<XYZ>();

            foreach (Curve c in outerLoop)
            {
                XYZ p = c.GetEndPoint(0);

                if (!vertices.Any(v => v.IsAlmostEqualTo(p)))
                    vertices.Add(p);
            }

            return vertices;
        }

        private static PlanarFace GetMainPlanarFace(Element element)
        {
            Options opt = new Options
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = false,
                DetailLevel = ViewDetailLevel.Fine
            };

            GeometryElement geom = element.get_Geometry(opt);
            if (geom == null)
                return null;

            List<Solid> solids = new List<Solid>();
            CollectSolids(geom, solids);

            if (solids.Count == 0)
                return null;

            PlanarFace bestFace = null;
            double bestArea = 0.0;

            foreach (Solid solid in solids)
            {
                if (solid == null || solid.Faces.IsEmpty)
                    continue;

                foreach (Face face in solid.Faces)
                {
                    if (face is PlanarFace planarFace)
                    {
                        if (planarFace.Area > bestArea)
                        {
                            bestArea = planarFace.Area;
                            bestFace = planarFace;
                        }
                    }
                }
            }

            return bestFace;
        }

        private static void CollectSolids(GeometryElement geom, List<Solid> solids)
        {
            if (geom == null)
                return;

            foreach (GeometryObject obj in geom)
            {
                if (obj is Solid solid)
                {
                    if (solid.Volume > 0)
                        solids.Add(solid);
                }
                else if (obj is GeometryInstance gi)
                {
                    GeometryElement instanceGeom = gi.GetInstanceGeometry();
                    CollectSolids(instanceGeom, solids);
                }
                else if (obj is GeometryElement nestedGeom)
                {
                    CollectSolids(nestedGeom, solids);
                }
            }
        }

        private static BoundingBoxXYZ GetBoundingBox(List<Element> elements)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            bool any = false;

            foreach (var e in elements)
            {
                var bb = e.get_BoundingBox(null);
                if (bb == null) continue;

                any = true;

                minX = System.Math.Min(minX, bb.Min.X);
                minY = System.Math.Min(minY, bb.Min.Y);
                minZ = System.Math.Min(minZ, bb.Min.Z);

                maxX = System.Math.Max(maxX, bb.Max.X);
                maxY = System.Math.Max(maxY, bb.Max.Y);
                maxZ = System.Math.Max(maxZ, bb.Max.Z);
            }

            if (!any) return null;

            return new BoundingBoxXYZ
            {
                Min = new XYZ(minX, minY, minZ),
                Max = new XYZ(maxX, maxY, maxZ)
            };
        }

        private static string ConvertLength(double v, string unit)
        {
            double result = unit switch
            {
                "mm" => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.Millimeters),
                "cm" => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.Centimeters),
                "m" => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.Meters),
                _ => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.Millimeters)
            };

            return result.ToString("0.###");
        }

        private static string ConvertArea(Parameter p, string unit)
        {
            double v = p.AsDouble();

            double result = unit switch
            {
                "mm2" => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.SquareMillimeters),
                "cm2" => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.SquareCentimeters),
                "m2" => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.SquareMeters),
                _ => UnitUtils.ConvertFromInternalUnits(v, UnitTypeId.SquareMeters)
            };

            return result.ToString("0.###");
        }

        private static string FindBuildingAddress(Document doc)
        {
            return doc?.ProjectInformation?.Address;
        }
    }
}