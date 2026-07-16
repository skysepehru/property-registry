using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Runs calculators over an already-ordered set of dirty nodes: for each node it resolves the output
    /// property instances from the store, builds a <see cref="CalculationContext"/> over the node's cached
    /// input filters and the store, invokes the calculator, and clears the outputs' dirty flag. The
    /// context resolves inputs lazily through the store, so evaluation allocates nothing on the hot path.
    /// </summary>
    internal sealed class CalculatorEvaluator : ICalculatorEvaluator
    {
        public void Evaluate(IReadOnlyList<PropertyGraphNode> sortedNodes, IPropertyStore store)
        {
            for (int n = 0; n < sortedNodes.Count; n++)
            {
                var node = sortedNodes[n];

                var outputs = store.GetEntityPropertyInstances(node.PropertyFilter.Entity, node.PropertyFilter.Name);

                var context = new CalculationContext(node.InputFilters, outputs, store);
                node.Calculator.Calculate(in context);

                for (int i = 0; i < outputs.Count; i++)
                {
                    outputs[i].IsDirtied = false;
                }
            }
        }
    }
}
