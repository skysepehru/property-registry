using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertyDependencyGraphTests
{
    // Hand-written double for IPropertyCalculator. NSubstitute can't proxy this interface because
    // GetInputProperties takes a Span<PropertyFilter> (a ByRef-like type Castle DynamicProxy rejects),
    // so a fake carrying a fixed output/input set is the correct tool for wiring the graph under test.
    internal sealed class FakeCalculator : IPropertyCalculator
    {
        private readonly PropertyFilter _output;
        private readonly PropertyFilter[] _inputs;

        public FakeCalculator(PropertyFilter output, PropertyFilter[] inputs)
        {
            _output = output;
            _inputs = inputs;
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
        {
        }
    }
}
