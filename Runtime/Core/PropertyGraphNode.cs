using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    internal class PropertyGraphNode
    {
        public PropertyFilter PropertyFilter;
        public PropertyFilter[] InputFilters;
        public List<PropertyGraphNode> Inputs;
        public List<PropertyGraphNode> Outputs;
        public IPropertyCalculator Calculator;
        public bool IsConnected => Calculator != null;
    }
}
