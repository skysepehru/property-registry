namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// The calculator attached to a base property's node: it has no inputs and never computes a value
    /// (base values are written directly via <see cref="PropertySystem{TEntity,TProperty}.SetBasePropertyValue"/>).
    /// It exists so every node has a calculator and so a node can be recognised as base.
    /// </summary>
    internal class BasePropertyCalculator : IPropertyCalculator
    {
        public readonly PropertyFilter _baseProperty;

        public BasePropertyCalculator(PropertyFilter baseFilter)
        {
            _baseProperty = baseFilter;
        }

        public void Declare(CalculatorBuilder builder)
        {
            builder.SetOutput(_baseProperty);
        }

        public void Calculate(in CalculationContext context)
        {
        }
    }
}
