using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// A writable view over the live instances of a calculator's output. Wraps the store's list without
    /// copying. Typical use: skip instances where <see cref="IsDirty"/> is false, and write results with
    /// <see cref="Set"/>.
    /// </summary>
    public readonly ref struct OutputGroup
    {
        private readonly List<Property> _instances;

        internal OutputGroup(List<Property> instances)
        {
            _instances = instances;
        }

        public int Count => _instances.Count;

        /// <summary>Whether the instance at <paramref name="index"/> was flagged dirty this pass.</summary>
        public bool IsDirty(int index) => _instances[index].IsDirtied;

        /// <summary>Writes the computed value for the instance at <paramref name="index"/>.</summary>
        public void Set(int index, double value) => _instances[index].ValueReactive.Value = value;

        /// <summary>The current value of the instance at <paramref name="index"/>.</summary>
        public double this[int index] => _instances[index].ReadOnlyValueReactive.CurrentValue;

        /// <summary>The full property at <paramref name="index"/>, for callers that need more than its value.</summary>
        public IReadOnlyProperty Get(int index) => _instances[index];
    }
}
