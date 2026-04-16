using System.Collections.Generic;

namespace BLComponentTemplate
{
    public class ComponentMatch
    {
        public string Category { get; set; }
        public string Family { get; set; }
        public string TypeName { get; set; }
        public string ElementId { get; set; }
        public int InstanceCount { get; set; }
        public string DimensionsSignature { get; set; }
        public bool IsIncludedInAssembly { get; set; } = true;
        public List<int> InstanceElementIds { get; set; } = new List<int>();
    }
}