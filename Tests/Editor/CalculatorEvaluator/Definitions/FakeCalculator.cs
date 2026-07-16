using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.CalculatorEvaluatorTests
{
    // Hand-written double for IPropertyCalculator. NSubstitute can't proxy this interface because
    // GetInputProperties takes a Span<PropertyFilter> (a ByRef-like type Castle DynamicProxy rejects),
    // so a fake is the correct tool here — its Calculate body is supplied per-test as a delegate.
    internal sealed class FakeCalculator : IPropertyCalculator
    {
        private readonly PropertyFilter _output;
        private readonly PropertyFilter[] _inputs;
        private readonly Action<IReadOnlyList<IReadOnlyList<IReadOnlyProperty>>, List<Property>> _calculate;

        public FakeCalculator(
            PropertyFilter output,
            PropertyFilter[] inputs,
            Action<IReadOnlyList<IReadOnlyList<IReadOnlyProperty>>, List<Property>> calculate)
        {
            _output = output;
            _inputs = inputs;
            _calculate = calculate;
        }

        public int GetInputProperties(Span<PropertyFilter> buffer)
        {
            for (int i = 0; i < _inputs.Length; i++)
            {
                buffer[i] = _inputs[i];
            }

            return _inputs.Length;
        }

        public PropertyFilter GetOutputProperty() => _output;

        public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputs, List<Property> outputs)
            => _calculate(inputs, outputs);
    }
}
