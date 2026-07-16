using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    internal class BasePropertyCalculator : IPropertyCalculator
    {
        public readonly PropertyFilter _baseProperty;

        public BasePropertyCalculator(PropertyFilter baseFilter)
        {
            _baseProperty = baseFilter;
        }

        public int GetInputProperties(Span<PropertyFilter> buffer) => 0;

        public PropertyFilter GetOutputProperty() => _baseProperty;

        public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputProperties, List<Property> outputProperties)
        {
        }
    }
}
