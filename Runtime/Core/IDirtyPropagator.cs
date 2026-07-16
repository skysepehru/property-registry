using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Dirty-propagation seam consumed by <see cref="PropertySystem{TEntity,TProperty}"/> so the
    /// orchestrator can be unit-tested against a substitute propagator.
    /// </summary>
    internal interface IDirtyPropagator
    {
        HashSet<PropertyGraphNode> DirtyNodes { get; }
        void ClearDirtyNodes();
        void MarkTreeNodeDirty(PropertyGraphNode node);
        void MarkPropertyDirty(PropertyId propertyId, PropertyGraphNode startNode, IPropertyStore store);
    }
}
