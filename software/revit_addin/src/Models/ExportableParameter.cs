namespace BLComponentTemplate
{
    public class ExportableParameter
    {
        public bool IsSelectedForExport { get; set; } = true;

        public string DisplayName { get; set; }

        public string Value { get; set; }

        public string Source { get; set; }

        public string Unit { get; set; }
    }
}