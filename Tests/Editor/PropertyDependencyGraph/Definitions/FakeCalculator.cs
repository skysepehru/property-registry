namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertyDependencyGraphTests
{
    // Hand-written double for IPropertyCalculator carrying a fixed output/input filter set. Declares them
    // through the builder's internal raw-filter overloads so the graph-wiring tests can drive Connect
    // directly with int-keyed filters; Calculate is unused here.
    internal sealed class FakeCalculator : IPropertyCalculator
    {
        private readonly PropertyFilter _output;
        private readonly PropertyFilter[] _inputs;

        public FakeCalculator(PropertyFilter output, PropertyFilter[] inputs)
        {
            _output = output;
            _inputs = inputs;
        }

        public void Declare(CalculatorBuilder builder)
        {
            for (int i = 0; i < _inputs.Length; i++)
            {
                builder.AddInput(_inputs[i]);
            }

            builder.SetOutput(_output);
        }

        public void Calculate(in CalculationContext context)
        {
        }
    }

    // A calculator that declares an input but never calls SetOutput, to exercise the graph rejecting a
    // calculator with no declared output at registration time.
    internal sealed class NoOutputCalculator : IPropertyCalculator
    {
        private readonly PropertyFilter _input;

        public NoOutputCalculator(PropertyFilter input)
        {
            _input = input;
        }

        public void Declare(CalculatorBuilder builder)
        {
            builder.AddInput(_input);
        }

        public void Calculate(in CalculationContext context)
        {
        }
    }
}
