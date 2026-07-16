using R3;

namespace skysepehru.Core.PropertyRegistry
{
    public class Property : IReadOnlyProperty
    {
        public ReadOnlyReactiveProperty<double> ReadOnlyValueReactive => ValueReactive;
        public PropertyId Id { get; }
        public ReactiveProperty<double> ValueReactive { get; }
        public bool IsDirtied { get; set; }

        internal Property(PropertyId id, double initialValue, bool isDirtied = true)
        {
            Id = id;
            ValueReactive = new ReactiveProperty<double>(initialValue);
            IsDirtied = isDirtied;
        }
    }
}
