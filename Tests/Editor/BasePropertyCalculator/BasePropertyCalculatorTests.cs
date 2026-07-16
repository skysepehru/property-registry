using System.Collections.Generic;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.BasePropertyCalculatorTests
{
    public class BasePropertyCalculatorTests
    {
        private const int Turret = 1;
        private const int Health = 10;
        private const double InitialValue = 42;

        [Test]
        public void Declare_SetsTheBaseFilterAsOutput_WithNoInputs()
        {
            var filter = PropertyFilter.New(Health, Turret);
            var calculator = new BasePropertyCalculator(filter);
            var builder = new CalculatorBuilder();

            calculator.Declare(builder);

            Assert.That(builder.OutputFilter.Equals(filter), Is.True);
            Assert.That(builder.BuildInputFilters(), Is.Empty);
        }

        [Test]
        public void BasePropertyCalculator_Calculate_IsANoOp()
        {
            var calculator = new BasePropertyCalculator(PropertyFilter.New(Health, Turret));
            var store = new PropertyStore(Turret);
            var output = store.Register(PropertyId.New(Health, Turret, 0), InitialValue);
            var outputs = new List<Property> { output };
            var context = new CalculationContext(new PropertyFilter[0], outputs, store);

            calculator.Calculate(in context);

            Assert.That(output.ReadOnlyValueReactive.CurrentValue, Is.EqualTo(InitialValue));
        }
    }
}
