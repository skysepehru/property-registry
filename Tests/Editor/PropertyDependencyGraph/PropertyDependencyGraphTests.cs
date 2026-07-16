using System;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertyDependencyGraphTests
{
    public class PropertyDependencyGraphTests
    {
        private const int Global = 0;
        private const int Turret = 1;
        private const int BaseInput = 10;
        private const int DerivedOutput = 11;
        private const int A = 20;
        private const int B = 21;
        private const int C = 22;
        private const int Join = 23;

        private static PropertyFilter Filter(int entity, int name) => PropertyFilter.New(name, entity);

        private PropertyDependencyGraph _graph;

        [SetUp]
        public void SetUp()
        {
            _graph = new PropertyDependencyGraph();
        }

        [Test]
        public void GetOrAddNode_BaseProperty_GetsBasePropertyCalculatorAndIsConnected()
        {
            var node = _graph.GetOrAddNode(Filter(Turret, BaseInput), isBaseProperty: true, out var wasAlreadyConnected);

            Assert.That(node.Calculator, Is.InstanceOf<BasePropertyCalculator>());
            Assert.That(node.IsConnected, Is.True);
            Assert.That(wasAlreadyConnected, Is.False);
        }

        [Test]
        public void GetOrAddNode_DerivedProperty_StartsWithoutCalculator()
        {
            var node = _graph.GetOrAddNode(Filter(Turret, DerivedOutput), isBaseProperty: false, out var wasAlreadyConnected);

            Assert.That(node.Calculator, Is.Null);
            Assert.That(node.IsConnected, Is.False);
            Assert.That(wasAlreadyConnected, Is.False);
        }

        [Test]
        public void GetOrAddNode_ExistingFilter_ReturnsSameNode_AndReportsConnectedState()
        {
            var first = _graph.GetOrAddNode(Filter(Turret, BaseInput), isBaseProperty: true, out _);

            var second = _graph.GetOrAddNode(Filter(Turret, BaseInput), isBaseProperty: true, out var wasAlreadyConnected);

            Assert.That(second, Is.SameAs(first));
            Assert.That(wasAlreadyConnected, Is.True);
        }

        [Test]
        public void GetOrAddNode_ExistingDerivedFilter_ReportsNotConnected()
        {
            _graph.GetOrAddNode(Filter(Turret, DerivedOutput), isBaseProperty: false, out _);

            _graph.GetOrAddNode(Filter(Turret, DerivedOutput), isBaseProperty: false, out var wasAlreadyConnected);

            Assert.That(wasAlreadyConnected, Is.False);
        }

        [Test]
        public void GetNode_Missing_Throws()
        {
            Assert.Catch(() => _graph.GetNode(Filter(Turret, BaseInput)));
        }

        [Test]
        public void Connect_WiresInputOutputEdges_AndConnectsTheOutputNode()
        {
            var inputNode = _graph.GetOrAddNode(Filter(Turret, BaseInput), isBaseProperty: true, out _);
            var outputNode = _graph.GetOrAddNode(Filter(Turret, DerivedOutput), isBaseProperty: false, out _);
            var calculator = new FakeCalculator(Filter(Turret, DerivedOutput), new[] { Filter(Turret, BaseInput) });

            var returned = _graph.Connect(calculator);

            Assert.That(returned, Is.SameAs(outputNode));
            Assert.That(outputNode.Calculator, Is.SameAs(calculator));
            Assert.That(outputNode.IsConnected, Is.True);
            Assert.That(inputNode.Outputs, Contains.Item(outputNode));
            Assert.That(outputNode.Inputs, Contains.Item(inputNode));
        }

        [Test]
        public void Connect_UnregisteredOutput_Throws()
        {
            _graph.GetOrAddNode(Filter(Turret, BaseInput), isBaseProperty: true, out _);
            var calculator = new FakeCalculator(Filter(Turret, DerivedOutput), new[] { Filter(Turret, BaseInput) });

            Assert.Catch(() => _graph.Connect(calculator));
        }

        [Test]
        public void Connect_UnregisteredInput_Throws()
        {
            _graph.GetOrAddNode(Filter(Turret, DerivedOutput), isBaseProperty: false, out _);
            var calculator = new FakeCalculator(Filter(Turret, DerivedOutput), new[] { Filter(Turret, BaseInput) });

            Assert.Catch(() => _graph.Connect(calculator));
        }

        [Test]
        public void Connect_OutputAlreadyConnected_Throws()
        {
            // A base property's node is already driven by its BasePropertyCalculator.
            _graph.GetOrAddNode(Filter(Turret, DerivedOutput), isBaseProperty: true, out _);
            var calculator = new FakeCalculator(Filter(Turret, DerivedOutput), Array.Empty<PropertyFilter>());

            Assert.Catch(() => _graph.Connect(calculator));
        }

        [Test]
        public void Connect_SelfDependency_Throws()
        {
            _graph.GetOrAddNode(Filter(Turret, A), isBaseProperty: false, out _);

            var calculator = new FakeCalculator(Filter(Turret, A), new[] { Filter(Turret, A) });

            Assert.Catch(() => _graph.Connect(calculator));
        }

        [Test]
        public void Connect_ClosingADirectCycle_Throws()
        {
            _graph.GetOrAddNode(Filter(Turret, A), isBaseProperty: false, out _);
            _graph.GetOrAddNode(Filter(Turret, B), isBaseProperty: false, out _);

            // B depends on A; then making A depend on B closes the loop.
            _graph.Connect(new FakeCalculator(Filter(Turret, B), new[] { Filter(Turret, A) }));

            Assert.Catch(() => _graph.Connect(new FakeCalculator(Filter(Turret, A), new[] { Filter(Turret, B) })));
        }

        [Test]
        public void Connect_ClosingAnIndirectCycle_Throws()
        {
            _graph.GetOrAddNode(Filter(Turret, A), isBaseProperty: false, out _);
            _graph.GetOrAddNode(Filter(Turret, B), isBaseProperty: false, out _);
            _graph.GetOrAddNode(Filter(Turret, C), isBaseProperty: false, out _);

            // A -> B -> C; then making A depend on C closes the loop.
            _graph.Connect(new FakeCalculator(Filter(Turret, B), new[] { Filter(Turret, A) }));
            _graph.Connect(new FakeCalculator(Filter(Turret, C), new[] { Filter(Turret, B) }));

            Assert.Catch(() => _graph.Connect(new FakeCalculator(Filter(Turret, A), new[] { Filter(Turret, C) })));
        }

        [Test]
        public void Connect_CalculatorWithoutOutput_Throws()
        {
            _graph.GetOrAddNode(Filter(Turret, BaseInput), isBaseProperty: true, out _);

            // Declares an input but never calls SetOutput — must be rejected at registration.
            Assert.Catch(() => _graph.Connect(new NoOutputCalculator(Filter(Turret, BaseInput))));
        }

        [Test]
        public void Connect_AcyclicDiamond_DoesNotThrow()
        {
            _graph.GetOrAddNode(Filter(Turret, BaseInput), isBaseProperty: true, out _);
            _graph.GetOrAddNode(Filter(Turret, A), isBaseProperty: false, out _);
            _graph.GetOrAddNode(Filter(Turret, B), isBaseProperty: false, out _);
            _graph.GetOrAddNode(Filter(Turret, Join), isBaseProperty: false, out _);

            // A and B both depend on BaseInput; Join depends on A and B. A diamond — no cycle.
            _graph.Connect(new FakeCalculator(Filter(Turret, A), new[] { Filter(Turret, BaseInput) }));
            _graph.Connect(new FakeCalculator(Filter(Turret, B), new[] { Filter(Turret, BaseInput) }));

            Assert.DoesNotThrow(() =>
                _graph.Connect(new FakeCalculator(Filter(Turret, Join), new[] { Filter(Turret, A), Filter(Turret, B) })));
        }
    }
}
