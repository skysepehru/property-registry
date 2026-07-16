using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Calculator-evaluation seam consumed by <see cref="PropertySystem{TEntity,TProperty}"/> so the
    /// orchestrator can be unit-tested against a substitute evaluator.
    /// </summary>
    internal interface ICalculatorEvaluator
    {
        void Evaluate(IReadOnlyList<PropertyGraphNode> sortedNodes, IPropertyStore store);
    }
}
