namespace BLComponentTemplate.Services.Export.Extractors
{
    public static class ComponentDataExtractorResolver
    {
        public static IComponentDataExtractor Resolve(string componentType, bool compositeExportMode)
        {
            return componentType switch
            {
                "Puertas cortafuegos" => new FireDoorDataExtractor(),
                "Pilares de acero" => new MetalSubcomponentDataExtractor(),
                "Vidrios" => new GlassDataExtractor(),
                "Lonas" => new TarpaulinDataExtractor(),
                "Bombas de calor" => new HeatPumpDataExtractor(),
                "Persianas enrollables" => new RollingDoorDataExtractor(),
                "Ventanas" => new WindowDataExtractor(),

                "Estructuras metálicas" => new MetalStructureDataExtractor(),
                "Sistemas de climatización" => new HvacSystemDataExtractor(),

                _ => new FireDoorDataExtractor()
            };
        }
    }
}