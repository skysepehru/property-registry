using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Topological-ordering seam consumed by <see cref="PropertySystem{TEntity,TProperty}"/> so the
    /// orchestrator can be unit-tested against a substitute sorter.
    /// </summary>
    internal interface IDirtySubgraphSorter
    {
        IReadOnlyList<PropertyGraphNode> Sort(IReadOnlyCollection<PropertyGraphNode> dirtyNodes);
    }
}
