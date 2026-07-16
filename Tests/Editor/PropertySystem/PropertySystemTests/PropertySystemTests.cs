using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertySystemTests
{
    // SUT: PropertySystem, fully isolated. All five subsystems (store, graph, propagator, sorter,
    // evaluator) are NSubstitute mocks injected through the internal test constructor, so these tests
    // pin only the orchestrator's own wiring and validation — a bug in any subsystem cannot fail them.
    // The end-to-end behaviour of the assembled system is covered by PropertySystemIntegrationTests.
    public class PropertySystemTests
    {
        private const double FiveValue = 5D;

        private IPropertyStore _store;
        private IPropertyDependencyGraph _graph;
        private IDirtyPropagator _propagator;
        private IDirtySubgraphSorter _sorter;
        private ICalculatorEvaluator _evaluator;

        private Property _property;
        private PropertyGraphNode _node;

        private TestPropertySystem _system;

        [SetUp]
        public void SetUp()
        {
            _store = Substitute.For<IPropertyStore>();
            _graph = Substitute.For<IPropertyDependencyGraph>();
            _propagator = Substitute.For<IDirtyPropagator>();
            _sorter = Substitute.For<IDirtySubgraphSorter>();
            _evaluator = Substitute.For<ICalculatorEvaluator>();

            _property = new Property(PropertyId.New(TestProperties.Base, TestEntities.Primary, 0), 0);
            _node = new PropertyGraphNode { PropertyFilter = PropertyFilter.New(TestProperties.Base, TestEntities.Primary) };

            // Non-null defaults so the orchestrator can dereference subsystem results. The out parameter
            // defaults to false (node was not already connected).
            _store.Register(Arg.Any<PropertyId>(), Arg.Any<double>()).Returns(_property);
            _store.GetProperty(Arg.Any<PropertyId>()).Returns(_property);
            _graph.GetOrAddNode(Arg.Any<PropertyFilter>(), Arg.Any<bool>(), out _).Returns(_node);
            _graph.GetNode(Arg.Any<PropertyFilter>()).Returns(_node);
            _graph.Connect(Arg.Any<IPropertyCalculator>()).Returns(_node);

            _system = new TestPropertySystem(_store, _graph, _propagator, _sorter, _evaluator);
        }

        // ---- Base / derived registration --------------------------------------------------------

        [Test]
        public void RegisterBaseInstancedProperty_RegistersTheBaseValueInTheStore()
        {
            _system.RegisterBaseInstancedProperty(TestProperties.Base, TestEntities.Primary, 0, FiveValue);

            _store.Received(1).Register(PropertyId.New(TestProperties.Base, TestEntities.Primary, 0), FiveValue);
        }

        [Test]
        public void RegisterBaseInstancedProperty_AddsABaseNodeToTheGraph()
        {
            _system.RegisterBaseInstancedProperty(TestProperties.Base, TestEntities.Primary, 0, FiveValue);

            _graph.Received(1).GetOrAddNode(
                PropertyFilter.New(TestProperties.Base, TestEntities.Primary), true, out _);
        }

        [Test]
        public void RegisterBaseInstancedProperty_ReturnsThePropertysReadOnlyReactive()
        {
            var result = _system.RegisterBaseInstancedProperty(TestProperties.Base, TestEntities.Primary, 0, FiveValue);

            Assert.That(result, Is.SameAs(_property.ReadOnlyValueReactive));
        }

        [Test]
        public void RegisterBaseInstancedProperty_ForTheGlobalEntity_Throws()
        {
            Assert.Catch(() => _system.RegisterBaseInstancedProperty(TestProperties.Base, TestEntities.Global, 0, FiveValue));
        }

        [Test]
        public void RegisterBaseGlobalProperty_RegistersUnderTheGlobalEntityAtInstanceZero()
        {
            _system.RegisterBaseGlobalProperty(TestProperties.BaseGlobal, FiveValue);

            _store.Received(1).Register(PropertyId.New(TestProperties.BaseGlobal, TestEntities.Global, 0), FiveValue);
        }

        [Test]
        public void RegisterDerivedInstancedProperty_RegistersWithZeroValueAndADerivedNode()
        {
            _system.RegisterDerivedInstancedProperty(TestProperties.Derived, TestEntities.Primary, 0);

            _store.Received(1).Register(PropertyId.New(TestProperties.Derived, TestEntities.Primary, 0), 0);
            _graph.Received(1).GetOrAddNode(
                PropertyFilter.New(TestProperties.Derived, TestEntities.Primary), false, out _);
        }

        [Test]
        public void RegisterDerivedInstancedProperty_ForTheGlobalEntity_Throws()
        {
            Assert.Catch(() => _system.RegisterDerivedInstancedProperty(TestProperties.Derived, TestEntities.Global, 0));
        }

        [Test]
        public void RegisterDerivedGlobalProperty_RegistersUnderTheGlobalEntityAsDerived()
        {
            _system.RegisterDerivedGlobalProperty(TestProperties.DerivedGlobal);

            _store.Received(1).Register(PropertyId.New(TestProperties.DerivedGlobal, TestEntities.Global, 0), 0);
            _graph.Received(1).GetOrAddNode(
                PropertyFilter.New(TestProperties.DerivedGlobal, TestEntities.Global), false, out _);
        }

        [Test]
        public void RegisterProperty_WhenTheNodeWasAlreadyConnected_MarksItsTreeDirty()
        {
            _graph.GetOrAddNode(Arg.Any<PropertyFilter>(), Arg.Any<bool>(), out _)
                .Returns(ci =>
                {
                    ci[2] = true; // wasAlreadyConnected
                    return _node;
                });

            _system.RegisterDerivedInstancedProperty(TestProperties.Derived, TestEntities.Primary, 0);

            _propagator.Received(1).MarkTreeNodeDirty(_node);
        }

        [Test]
        public void RegisterProperty_WhenTheNodeWasNotAlreadyConnected_DoesNotMarkDirty()
        {
            _system.RegisterDerivedInstancedProperty(TestProperties.Derived, TestEntities.Primary, 0);

            _propagator.DidNotReceive().MarkTreeNodeDirty(Arg.Any<PropertyGraphNode>());
        }

        // ---- Calculator registration ------------------------------------------------------------

        [Test]
        public void RegisterCalculator_ConnectsItInTheGraphAndMarksTheReturnedNodeDirty()
        {
            var calculator = new FakeCalculator();
            _graph.Connect(calculator).Returns(_node);

            _system.RegisterCalculator(calculator);

            _graph.Received(1).Connect(calculator);
            _propagator.Received(1).MarkTreeNodeDirty(_node);
        }

        [Test]
        public void RegisterCalculator_Generic_ConstructsAndConnectsTheCalculator()
        {
            _system.RegisterCalculator<FakeCalculator>();

            _graph.Received(1).Connect(Arg.Any<FakeCalculator>());
        }

        // ---- SetBasePropertyValue ---------------------------------------------------------------

        [Test]
        public void SetBasePropertyValue_OnABaseProperty_WritesTheValueToTheStoredProperty()
        {
            var baseFilter = PropertyFilter.New(TestProperties.Base, TestEntities.Primary);
            _graph.GetNode(baseFilter).Returns(new PropertyGraphNode
            {
                PropertyFilter = baseFilter,
                IsBaseProperty = true,
                Calculator = new BasePropertyCalculator(baseFilter),
            });
            var stored = new Property(PropertyId.New(TestProperties.Base, TestEntities.Primary, 0), 0);
            _store.GetProperty(PropertyId.New(TestProperties.Base, TestEntities.Primary, 0)).Returns(stored);

            _system.SetBasePropertyValue(TestProperties.Base, TestEntities.Primary, FiveValue, 0);

            Assert.That(stored.ValueReactive.Value, Is.EqualTo(FiveValue));
        }

        [Test]
        public void SetBasePropertyValue_OnABaseProperty_MarksThePropertyAndItsTreeDirty()
        {
            var baseFilter = PropertyFilter.New(TestProperties.Base, TestEntities.Primary);
            var baseNode = new PropertyGraphNode
            {
                PropertyFilter = baseFilter,
                IsBaseProperty = true,
                Calculator = new BasePropertyCalculator(baseFilter),
            };
            _graph.GetNode(baseFilter).Returns(baseNode);

            _system.SetBasePropertyValue(TestProperties.Base, TestEntities.Primary, FiveValue, 0);

            _propagator.Received(1).MarkPropertyDirty(
                PropertyId.New(TestProperties.Base, TestEntities.Primary, 0), baseNode, _store);
            _propagator.Received(1).MarkTreeNodeDirty(baseNode);
        }

        [Test]
        public void SetBasePropertyValue_OnADerivedProperty_Throws()
        {
            var derivedFilter = PropertyFilter.New(TestProperties.Derived, TestEntities.Primary);
            // A node not flagged as base is a derived property and must reject direct writes.
            _graph.GetNode(derivedFilter).Returns(new PropertyGraphNode
            {
                PropertyFilter = derivedFilter,
                Calculator = new FakeCalculator(),
            });

            Assert.Catch(() => _system.SetBasePropertyValue(TestProperties.Derived, TestEntities.Primary, FiveValue, 0));
        }

        // ---- Tick + indexer ---------------------------------------------------------------------

        [Test]
        public void Tick_SortsTheDirtyNodes_EvaluatesThem_ThenClearsTheDirtySet()
        {
            var dirty = new HashSet<PropertyGraphNode> { _node };
            var sorted = new List<PropertyGraphNode> { _node };
            _propagator.DirtyNodes.Returns(dirty);
            _sorter.Sort(dirty).Returns(sorted);

            _system.Tick();

            _sorter.Received(1).Sort(dirty);
            _evaluator.Received(1).Evaluate(sorted, _store);
            _propagator.Received(1).ClearDirtyNodes();
        }

        [Test]
        public void Indexer_ReturnsTheStoredPropertysReadOnlyReactive()
        {
            var stored = new Property(PropertyId.New(TestProperties.Base, TestEntities.Primary, 0), FiveValue);
            _store.GetProperty(PropertyId.New(TestProperties.Base, TestEntities.Primary, 0)).Returns(stored);

            var result = _system[TestProperties.Base, TestEntities.Primary, 0];

            Assert.That(result, Is.SameAs(stored.ReadOnlyValueReactive));
        }
    }
}
