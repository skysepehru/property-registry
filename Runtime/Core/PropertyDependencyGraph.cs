using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Owns the calculator dependency graph: one <see cref="PropertyGraphNode"/> per
    /// (entity, property) filter, plus the wiring + validation when a calculator is connected.
    /// </summary>
    internal sealed class PropertyDependencyGraph : IPropertyDependencyGraph
    {
        private readonly Dictionary<PropertyFilter, PropertyGraphNode> _nodes = new();

        private readonly Queue<PropertyGraphNode> _cycleQueueCache = new();
        private readonly HashSet<PropertyGraphNode> _cycleVisitedCache = new();

        /// <summary>
        /// Returns the node for <paramref name="filter"/>, creating it if absent. Base properties get
        /// a <see cref="BasePropertyCalculator"/>. <paramref name="wasAlreadyConnected"/> reports
        /// whether an existing node already had a calculator (so the caller can mark it dirty).
        /// </summary>
        public PropertyGraphNode GetOrAddNode(PropertyFilter filter, bool isBaseProperty, out bool wasAlreadyConnected)
        {
            if (_nodes.TryGetValue(filter, out var node))
            {
                wasAlreadyConnected = node.IsConnected;
                return node;
            }

            node = new PropertyGraphNode { PropertyFilter = filter };
            if (isBaseProperty)
            {
                node.IsBaseProperty = true;
                node.Calculator = new BasePropertyCalculator(filter);
                node.InputFilters = Array.Empty<PropertyFilter>();
            }

            _nodes[filter] = node;
            wasAlreadyConnected = false;
            return node;
        }

        public PropertyGraphNode GetNode(PropertyFilter filter) => _nodes[filter];

        /// <summary>
        /// Validates a calculator's inputs/output are registered (and the output isn't already
        /// connected), wires the input→output edges, and returns the now-connected output node.
        /// </summary>
        public PropertyGraphNode Connect(IPropertyCalculator calculator)
        {
            var builder = new CalculatorBuilder();
            calculator.Declare(builder);

            PropertyFilter[] inputFilters = builder.BuildInputFilters();
            PropertyFilter outputFilter = builder.OutputFilter;

            if (!_nodes.TryGetValue(outputFilter, out var node))
            {
                throw new InvalidOperationException($"Calculator defines non-registered output property: {outputFilter.ToString()}.");
            }

            if (node.IsConnected)
            {
                throw new InvalidOperationException("A calculator is already registered for this property.");
            }

            for (int i = 0; i < inputFilters.Length; i++)
            {
                if (!_nodes.ContainsKey(inputFilters[i]))
                {
                    throw new InvalidOperationException("Calculator defines non-registered input properties.");
                }
            }

            if (WouldCreateCycle(node, inputFilters))
            {
                throw new InvalidOperationException("Connecting this calculator would create a cyclic dependency.");
            }

            node.Calculator = calculator;
            node.InputFilters = inputFilters;

            for (int i = 0; i < inputFilters.Length; i++)
            {
                var input = _nodes[inputFilters[i]];
                (input.Outputs ??= new List<PropertyGraphNode>()).Add(node);
                (node.Inputs ??= new List<PropertyGraphNode>()).Add(input);
            }

            return node;
        }

        /// <summary>
        /// The graph is acyclic before each <see cref="Connect"/>, and every edge a connect adds points
        /// into <paramref name="output"/>. So a new cycle can only form if one of the calculator's
        /// inputs is already reachable downstream of <paramref name="output"/> — then wiring
        /// input→output would close the loop. A self-dependency (an input equal to the output) is the
        /// degenerate, zero-length case and is caught the same way.
        /// </summary>
        private bool WouldCreateCycle(PropertyGraphNode output, PropertyFilter[] inputFilters)
        {
            _cycleQueueCache.Clear();
            _cycleVisitedCache.Clear();
            _cycleQueueCache.Enqueue(output);

            while (_cycleQueueCache.Count > 0)
            {
                var node = _cycleQueueCache.Dequeue();
                if (!_cycleVisitedCache.Add(node))
                {
                    continue;
                }

                for (int i = 0; i < inputFilters.Length; i++)
                {
                    if (node.PropertyFilter.Equals(inputFilters[i]))
                    {
                        return true;
                    }
                }

                if (node.Outputs == null)
                {
                    continue;
                }

                foreach (var downstream in node.Outputs)
                {
                    _cycleQueueCache.Enqueue(downstream);
                }
            }

            return false;
        }
    }
}
