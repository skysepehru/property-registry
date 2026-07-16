using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Storage + registration-validation seam consumed by <see cref="PropertySystem{TEntity,TProperty}"/>,
    /// <see cref="DirtyPropagator"/> and <see cref="CalculatorEvaluator"/>. Exists so those collaborators
    /// can be unit-tested against a substitute store in isolation.
    /// </summary>
    internal interface IPropertyStore
    {
        int Count { get; }
        IEnumerable<KeyValuePair<PropertyId, Property>> All { get; }
        Property Register(PropertyId id, double initialValue);
        Property GetProperty(PropertyId id);
        List<Property> GetEntityPropertyInstances(int entity, int name);
    }
}
