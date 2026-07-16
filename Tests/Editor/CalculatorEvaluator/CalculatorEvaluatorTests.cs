using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.CalculatorEvaluatorTests
{
    // SUT: CalculatorEvaluator, isolated. Its only stateful collaborator, the property store, is an
    // IPropertyStore substitute; the calculator is a hand fake (see Definitions/FakeCalculator) because
    // its Span parameter blocks NSubstitute. A bug in the real PropertyStore cannot fail these tests.
    public class CalculatorEvaluatorTests
    {
        private const int Turret = 1;
        private const int Level = 10;
        private const int Doubled = 11;
        private const int Mid = 12;
        private const int Final = 13;

        private const double Level0Value = 3;
        private const double Level1Value = 4;
        private const double SingleLevelValue = 5;
        private const double DoubleFactor = 2;
        private const double FinalIncrement = 1;
        private const int ThreeInstances = 3;

        private IPropertyStore _store;
        private CalculatorEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            _store = Substitute.For<IPropertyStore>();
            _evaluator = new CalculatorEvaluator();
        }

        private static PropertyFilter Filter(int name) => PropertyFilter.New(name, Turret);
        private static PropertyId Id(int name, int instance) => PropertyId.New(name, Turret, instance);
        private static Property Prop(int name, int instance, double value) => new(Id(name, instance), value);

        private static PropertyGraphNode NodeFor(IPropertyCalculator calculator)
            => new() { PropertyFilter = calculator.GetOutputProperty(), Calculator = calculator };

        // Wires the substitute store so a lookup of (Turret, name) returns exactly these instances.
        private void StoreReturns(int name, params Property[] instances)
            => _store.GetEntityPropertyInstances(Turret, name).Returns(new List<Property>(instances));

        [Test]
        public void Evaluate_ComputesOutputs_PerInstance_FromInputs()
        {
            var levels = new[] { Prop(Level, 0, Level0Value), Prop(Level, 1, Level1Value) };
            var doubled = new[] { Prop(Doubled, 0, 0), Prop(Doubled, 1, 0) };
            StoreReturns(Level, levels);
            StoreReturns(Doubled, doubled);

            var node = NodeFor(new FakeCalculator(Filter(Doubled), new[] { Filter(Level) },
                (inputs, outputs) =>
                {
                    var input = inputs[0];
                    for (int i = 0; i < outputs.Count; i++)
                    {
                        outputs[i].ValueReactive.Value = input[i].ReadOnlyValueReactive.CurrentValue * DoubleFactor;
                    }
                }));

            _evaluator.Evaluate(new[] { node }, _store);

            Assert.That(doubled[0].ReadOnlyValueReactive.CurrentValue, Is.EqualTo(Level0Value * DoubleFactor));
            Assert.That(doubled[1].ReadOnlyValueReactive.CurrentValue, Is.EqualTo(Level1Value * DoubleFactor));
        }

        [Test]
        public void Evaluate_ClearsTheDirtyFlagOnOutputs()
        {
            var output = Prop(Doubled, 0, 0); // Property is dirtied by default.
            StoreReturns(Level, Prop(Level, 0, Level0Value));
            StoreReturns(Doubled, output);

            var node = NodeFor(new FakeCalculator(Filter(Doubled), new[] { Filter(Level) }, (_, _) => { }));

            _evaluator.Evaluate(new[] { node }, _store);

            Assert.That(output.IsDirtied, Is.False);
        }

        [Test]
        public void Evaluate_RunsNodesInTheGivenOrder_SeeingUpstreamResults()
        {
            var level = Prop(Level, 0, SingleLevelValue);
            var mid = Prop(Mid, 0, 0);
            var final = Prop(Final, 0, 0);
            StoreReturns(Level, level);
            StoreReturns(Mid, mid);
            StoreReturns(Final, final);

            // Mid = Level * DoubleFactor; Final = Mid + FinalIncrement. Final must see Mid from the same pass.
            var midNode = NodeFor(new FakeCalculator(Filter(Mid), new[] { Filter(Level) },
                (inputs, outputs) => outputs[0].ValueReactive.Value = inputs[0][0].ReadOnlyValueReactive.CurrentValue * DoubleFactor));
            var finalNode = NodeFor(new FakeCalculator(Filter(Final), new[] { Filter(Mid) },
                (inputs, outputs) => outputs[0].ValueReactive.Value = inputs[0][0].ReadOnlyValueReactive.CurrentValue + FinalIncrement));

            _evaluator.Evaluate(new[] { midNode, finalNode }, _store);

            Assert.That(mid.ReadOnlyValueReactive.CurrentValue, Is.EqualTo(SingleLevelValue * DoubleFactor));
            Assert.That(final.ReadOnlyValueReactive.CurrentValue, Is.EqualTo(SingleLevelValue * DoubleFactor + FinalIncrement));
        }

        [Test]
        public void Evaluate_PassesEveryInputInstanceToTheCalculator()
        {
            StoreReturns(Level, Prop(Level, 0, 0), Prop(Level, 1, 0), Prop(Level, 2, 0));
            StoreReturns(Doubled, Prop(Doubled, 0, 0));

            var seenInputCount = -1;
            var node = NodeFor(new FakeCalculator(Filter(Doubled), new[] { Filter(Level) },
                (inputs, _) => seenInputCount = inputs[0].Count));

            _evaluator.Evaluate(new[] { node }, _store);

            Assert.That(seenInputCount, Is.EqualTo(ThreeInstances));
        }
    }
}
