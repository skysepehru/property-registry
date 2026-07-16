using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// The per-node argument to <see cref="IPropertyCalculator.Calculate"/>. Resolves declared inputs and
    /// the output against the live property store on demand, keyed by the <see cref="InputHandle"/>s the
    /// calculator obtained in <see cref="IPropertyCalculator.Declare"/>. A stack-only ref struct that
    /// allocates nothing per tick.
    /// </summary>
    public readonly ref struct CalculationContext
    {
        private readonly PropertyFilter[] _inputFilters;
        private readonly List<Property> _outputs;
        private readonly IPropertyStore _store;

        internal CalculationContext(PropertyFilter[] inputFilters, List<Property> outputs, IPropertyStore store)
        {
            _inputFilters = inputFilters;
            _outputs = outputs;
            _store = store;
        }

        /// <summary>Resolves the instances of the input referenced by <paramref name="handle"/>.</summary>
        public InputGroup Inputs(InputHandle handle)
        {
            var filter = _inputFilters[handle.Index];
            return new InputGroup(_store.GetEntityPropertyInstances(filter.Entity, filter.Name));
        }

        /// <summary>
        /// Convenience for a single-instance (typically global) input: the current value of its one
        /// instance. Throws if the input resolves to more than one instance, to catch accidental use on a
        /// per-instance input.
        /// </summary>
        public double Value(InputHandle handle)
        {
            var filter = _inputFilters[handle.Index];
            var instances = _store.GetEntityPropertyInstances(filter.Entity, filter.Name);
            if (instances.Count != 1)
            {
                throw new InvalidOperationException(
                    "CalculationContext.Value expects a single-instance input; use Inputs(handle) for per-instance inputs.");
            }

            return instances[0].ReadOnlyValueReactive.CurrentValue;
        }

        /// <summary>The instances of this calculator's output property.</summary>
        public OutputGroup Outputs => new OutputGroup(_outputs);
    }
}
