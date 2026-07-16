using System;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.CalculatorBuilderTests
{
    // Covers the declare-time surface: CalculatorBuilder and InputHandle.
    public class CalculatorBuilderTests
    {
        private const int Turret = 1;
        private const int Strength = 10;
        private const int Multiplier = 11;
        private const int AttackPower = 12;

        private static PropertyFilter Filter(int name) => PropertyFilter.New(name, Turret);

        [Test]
        public void UninitializedInputHandle_Throws_WhenResolved()
        {
            var store = new PropertyStore(0);
            var outputs = new System.Collections.Generic.List<Property>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                var context = new CalculationContext(new PropertyFilter[0], outputs, store);
                context.Inputs(default);
            });
        }

        [Test]
        public void AddInput_ReturnsHandlesInDeclarationOrder()
        {
            var builder = new CalculatorBuilder();

            var strength = builder.AddInput(Filter(Strength));
            var multiplier = builder.AddInput(Filter(Multiplier));
            builder.SetOutput(Filter(AttackPower));

            var filters = builder.BuildInputFilters();
            Assert.That(filters.Length, Is.EqualTo(2));
            Assert.That(filters[strength.Index].Equals(Filter(Strength)), Is.True);
            Assert.That(filters[multiplier.Index].Equals(Filter(Multiplier)), Is.True);
        }

        [Test]
        public void SetOutput_CalledTwice_Throws()
        {
            var builder = new CalculatorBuilder();
            builder.SetOutput(Filter(AttackPower));

            Assert.Throws<InvalidOperationException>(() => builder.SetOutput(Filter(Strength)));
        }

        [Test]
        public void OutputFilter_WithoutSetOutput_Throws()
        {
            var builder = new CalculatorBuilder();
            builder.AddInput(Filter(Strength));

            Assert.Throws<InvalidOperationException>(() => _ = builder.OutputFilter);
        }
    }
}
