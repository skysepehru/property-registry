using System;
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
        public void GetOutputProperty_ReturnsTheBaseFilter()
        {
            var filter = PropertyFilter.New(Health, Turret);
            var calculator = new BasePropertyCalculator(filter);

            Assert.That(calculator.GetOutputProperty().Equals(filter), Is.True);
        }

        [Test]
        public void GetInputProperties_ReturnsZero()
        {
            var calculator = new BasePropertyCalculator(PropertyFilter.New(Health, Turret));
            Span<PropertyFilter> buffer = stackalloc PropertyFilter[4];

            Assert.That(calculator.GetInputProperties(buffer), Is.EqualTo(0));
        }

        [Test]
        public void BasePropertyCalculator_Calculate_IsANoOp()
        {
            var calculator = new BasePropertyCalculator(PropertyFilter.New(Health, Turret));
            var output = new Property(PropertyId.New(Health, Turret, 0), InitialValue);
            var outputs = new List<Property> { output };
            var inputs = new List<IReadOnlyList<IReadOnlyProperty>>();

            Assert.DoesNotThrow(() => calculator.Calculate(inputs, outputs));
            Assert.That(output.ReadOnlyValueReactive.CurrentValue, Is.EqualTo(InitialValue));
        }
    }
}
