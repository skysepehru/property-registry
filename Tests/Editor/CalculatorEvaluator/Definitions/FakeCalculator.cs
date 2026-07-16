using System;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.CalculatorEvaluatorTests
{
    // Hand-written double for IPropertyCalculator. Both bodies are supplied per-test as delegates:
    // Declare (to capture input handles) and Calculate. Because CalculationContext is a ref struct it
    // can't be an Action<T>, so Calculate uses the custom CalculateDelegate below.
    internal sealed class FakeCalculator : IPropertyCalculator
    {
        internal delegate void CalculateDelegate(in CalculationContext context);

        private readonly Action<CalculatorBuilder> _declare;
        private readonly CalculateDelegate _calculate;

        public FakeCalculator(Action<CalculatorBuilder> declare, CalculateDelegate calculate)
        {
            _declare = declare;
            _calculate = calculate;
        }

        public void Declare(CalculatorBuilder builder) => _declare(builder);

        public void Calculate(in CalculationContext context) => _calculate(in context);
    }
}
