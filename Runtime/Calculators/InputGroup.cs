using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// A read-only view over the live instances of one declared input: a single element for a global
    /// property, one element per instance for a per-instance property. Wraps the store's list without
    /// copying, so it is safe to use on the hot path.
    /// </summary>
    public readonly ref struct InputGroup
    {
        private readonly List<Property> _instances;

        internal InputGroup(List<Property> instances)
        {
            _instances = instances;
        }

        public int Count => _instances.Count;

        /// <summary>The current value of the instance at <paramref name="index"/>.</summary>
        public double this[int index] => _instances[index].ReadOnlyValueReactive.CurrentValue;

        /// <summary>The full property at <paramref name="index"/>, for callers that need more than its value.</summary>
        public IReadOnlyProperty Get(int index) => _instances[index];
    }
}
