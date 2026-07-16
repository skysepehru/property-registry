namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Dependency-graph seam consumed by <see cref="PropertySystem{TEntity,TProperty}"/> so the
    /// orchestrator can be unit-tested against a substitute graph.
    /// </summary>
    internal interface IPropertyDependencyGraph
    {
        PropertyGraphNode GetOrAddNode(PropertyFilter filter, bool isBaseProperty, out bool wasAlreadyConnected);
        PropertyGraphNode GetNode(PropertyFilter filter);
        PropertyGraphNode Connect(IPropertyCalculator calculator);
    }
}
