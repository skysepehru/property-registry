using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.CalculatorEvaluatorTests
{
    // SUT: CalculatorEvaluator, isolated. Its only stateful collaborator, the property store, is an
    // IPropertyStore substitute; the calculator is a hand fake (see Definitions/FakeCalculator) because
    // its ref-struct context blocks NSubstitute. A bug in the real PropertyStore cannot fail these tests.
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

        // Builds a node from a fake by running Declare (so InputFilters/PropertyFilter mirror what the
        // graph would have cached) and attaching the fake as the node's calculator.
        private static PropertyGraphNode NodeFor(PropertyFilter output, FakeCalculator calculator)
        {
            var builder = new CalculatorBuilder();
            calculator.Declare(builder);
            return new PropertyGraphNode
            {
                PropertyFilter = builder.OutputFilter,
                InputFilters = builder.BuildInputFilters(),
                Calculator = calculator,
            };
        }

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

            var levelHandle = default(InputHandle);
            var calculator = new FakeCalculator(
                b =>
                {
                    levelHandle = b.AddInput(Filter(Level));
                    b.SetOutput(Filter(Doubled));
                },
                (in CalculationContext ctx) =>
                {
                    var input = ctx.Inputs(levelHandle);
                    var outputs = ctx.Outputs;
                    for (int i = 0; i < outputs.Count; i++)
                    {
                        outputs.Set(i, input[i] * DoubleFactor);
                    }
                });
            var node = NodeFor(Filter(Doubled), calculator);

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

            var calculator = new FakeCalculator(
                b =>
                {
                    b.AddInput(Filter(Level));
                    b.SetOutput(Filter(Doubled));
                },
                (in CalculationContext ctx) => { });
            var node = NodeFor(Filter(Doubled), calculator);

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
            var levelHandle = default(InputHandle);
            var midCalculator = new FakeCalculator(
                b =>
                {
                    levelHandle = b.AddInput(Filter(Level));
                    b.SetOutput(Filter(Mid));
                },
                (in CalculationContext ctx) => ctx.Outputs.Set(0, ctx.Inputs(levelHandle)[0] * DoubleFactor));
            var midNode = NodeFor(Filter(Mid), midCalculator);

            var midHandle = default(InputHandle);
            var finalCalculator = new FakeCalculator(
                b =>
                {
                    midHandle = b.AddInput(Filter(Mid));
                    b.SetOutput(Filter(Final));
                },
                (in CalculationContext ctx) => ctx.Outputs.Set(0, ctx.Inputs(midHandle)[0] + FinalIncrement));
            var finalNode = NodeFor(Filter(Final), finalCalculator);

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
            var levelHandle = default(InputHandle);
            var calculator = new FakeCalculator(
                b =>
                {
                    levelHandle = b.AddInput(Filter(Level));
                    b.SetOutput(Filter(Doubled));
                },
                (in CalculationContext ctx) => seenInputCount = ctx.Inputs(levelHandle).Count);
            var node = NodeFor(Filter(Doubled), calculator);

            _evaluator.Evaluate(new[] { node }, _store);

            Assert.That(seenInputCount, Is.EqualTo(ThreeInstances));
        }

        [Test]
        public void Evaluate_ResolvesInputsByHandle_RegardlessOfDeclarationOrder()
        {
            // Declaring the level input second (after an unused global) must not change what the handle
            // resolves to: reads go through the handle, not a positional index.
            var levels = new[] { Prop(Level, 0, Level0Value), Prop(Level, 1, Level1Value) };
            var doubled = new[] { Prop(Doubled, 0, 0), Prop(Doubled, 1, 0) };
            StoreReturns(Level, levels);
            StoreReturns(Doubled, doubled);
            StoreReturns(Mid, Prop(Mid, 0, 0)); // an extra, unused input declared first

            var levelHandle = default(InputHandle);
            var calculator = new FakeCalculator(
                b =>
                {
                    b.AddInput(Filter(Mid));                 // handle 0, never read
                    levelHandle = b.AddInput(Filter(Level)); // handle 1, read below
                    b.SetOutput(Filter(Doubled));
                },
                (in CalculationContext ctx) =>
                {
                    var input = ctx.Inputs(levelHandle);
                    var outputs = ctx.Outputs;
                    for (int i = 0; i < outputs.Count; i++)
                    {
                        outputs.Set(i, input[i] * DoubleFactor);
                    }
                });
            var node = NodeFor(Filter(Doubled), calculator);

            _evaluator.Evaluate(new[] { node }, _store);

            Assert.That(doubled[0].ReadOnlyValueReactive.CurrentValue, Is.EqualTo(Level0Value * DoubleFactor));
            Assert.That(doubled[1].ReadOnlyValueReactive.CurrentValue, Is.EqualTo(Level1Value * DoubleFactor));
        }
    }
}
