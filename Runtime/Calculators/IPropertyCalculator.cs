using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    public interface IPropertyCalculator
    {
        int GetInputProperties(Span<PropertyFilter> buffer);
        PropertyFilter GetOutputProperty();
        void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputProperties, List<Property> outputProperties);
    }
}
