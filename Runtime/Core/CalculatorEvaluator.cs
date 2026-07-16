using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Runs calculators over an already-ordered set of dirty nodes: gathers each calculator's input
    /// and output property instances from the store, invokes it, and clears the outputs' dirty flag.
    /// Owns its reusable input/output buffers so evaluation allocates nothing on the hot path.
    /// </summary>
    internal sealed class CalculatorEvaluator : ICalculatorEvaluator
    {
        private const int MAX_CALCULATOR_INPUT = 32;

        private readonly List<List<Property>> _inputCache;
        private readonly List<Property> _outputCache = new();

        public CalculatorEvaluator()
        {
            _inputCache = new List<List<Property>>();
            for (int i = 0; i < MAX_CALCULATOR_INPUT; i++)
            {
                _inputCache.Add(new List<Property>());
            }
        }

        public void Evaluate(IReadOnlyList<PropertyGraphNode> sortedNodes, IPropertyStore store)
        {
            Span<PropertyFilter> inputFilters = stackalloc PropertyFilter[MAX_CALCULATOR_INPUT];

            for (int n = 0; n < sortedNodes.Count; n++)
            {
                var node = sortedNodes[n];
                ClearInputCache();
                _outputCache.Clear();

                var inputFilterCount = node.Calculator.GetInputProperties(inputFilters);
                for (int i = 0; i < inputFilterCount; i++)
                {
                    _inputCache[i].AddRange(store.GetEntityPropertyInstances(inputFilters[i].Entity, inputFilters[i].Name));
                }

                var outputFilter = node.Calculator.GetOutputProperty();
                _outputCache.AddRange(store.GetEntityPropertyInstances(outputFilter.Entity, outputFilter.Name));

                node.Calculator.Calculate(_inputCache, _outputCache);

                for (int i = 0; i < _outputCache.Count; i++)
                {
                    _outputCache[i].IsDirtied = false;
                }
            }
        }

        private void ClearInputCache()
        {
            for (int i = 0; i < MAX_CALCULATOR_INPUT; i++)
            {
                _inputCache[i].Clear();
            }
        }
    }
}
