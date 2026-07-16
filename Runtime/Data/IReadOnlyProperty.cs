using R3;

namespace skysepehru.Core.PropertyRegistry
{
    public interface IReadOnlyProperty
    {
        public PropertyId Id { get; }
        public ReadOnlyReactiveProperty<double> ReadOnlyValueReactive { get; }
    }
}
