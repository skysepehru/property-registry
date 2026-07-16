using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.DirtyPropagatorTests
{
    // SUT: DirtyPropagator, isolated. MarkTreeNodeDirty walks the node graph only (no store). The
    // MarkPropertyDirty tests drive an IPropertyStore substitute: Register() seeds clean Property
    // instances into it and keeps a handle so the resulting dirty flags can be asserted.
    public class DirtyPropagatorTests
    {
        private const int Global = 0;
        private const int Turret = 1;
        private const int Container = 2;

        private const int Level = 10;
        private const int Derived = 11;
        private const int TurretDps = 12;
        private const int Storage = 13;
        private const int DownstreamDerived = 14;
        private const int GlobalMultiplier = 20;
        private const int TotalDps = 21;

        private const int OneInstance = 1;
        private const int TwoInstances = 2;

        private DirtyPropagator _propagator;
        private IPropertyStore _store;
        private Dictionary<PropertyId, Property> _properties;

        [SetUp]
        public void SetUp()
        {
            _propagator = new DirtyPropagator(Global);
            _store = Substitute.For<IPropertyStore>();
            _properties = new Dictionary<PropertyId, Property>();
        }

        // Seeds `count` clean instances of (entity, name) into the substitute store and remembers each
        // so Dirty(...) can read its flag back after propagation.
        private void Register(int entity, int name, int count)
        {
            var instances = new List<Property>(count);
            for (int i = 0; i < count; i++)
            {
                var id = PropertyId.New(name, entity, i);
                var property = new Property(id, 0, isDirtied: false);
                _properties[id] = property;
                _store.GetProperty(id).Returns(property);
                instances.Add(property);
            }

            _store.GetEntityPropertyInstances(entity, name).Returns(instances);
        }

        private bool Dirty(int name, int entity, int instance)
            => _properties[PropertyId.New(name, entity, instance)].IsDirtied;

        private static PropertyGraphNode Node(int entity = 0, int name = 0)
            => new() { PropertyFilter = PropertyFilter.New(name, entity) };

        private static void AddOutput(PropertyGraphNode from, PropertyGraphNode to)
            => (from.Outputs ??= new List<PropertyGraphNode>()).Add(to);

        // ---- MarkTreeNodeDirty ------------------------------------------------------------------

        [Test]
        public void MarkTreeNodeDirty_MarksTheNodeAndAllDownstreamNodes()
        {
            var a = Node();
            var b = Node();
            var c = Node();
            AddOutput(a, b);
            AddOutput(b, c);

            _propagator.MarkTreeNodeDirty(a);

            Assert.That(_propagator.DirtyNodes, Is.EquivalentTo(new[] { a, b, c }));
        }

        [Test]
        public void MarkTreeNodeDirty_LeafNode_MarksOnlyItself()
        {
            var leaf = Node();

            _propagator.MarkTreeNodeDirty(leaf);

            Assert.That(_propagator.DirtyNodes, Is.EquivalentTo(new[] { leaf }));
        }

        [Test]
        public void MarkTreeNodeDirty_IsIdempotentAcrossOverlappingSubgraphs()
        {
            var a = Node();
            var b = Node();
            var c = Node();
            AddOutput(a, b);
            AddOutput(b, c);

            _propagator.MarkTreeNodeDirty(a);
            _propagator.MarkTreeNodeDirty(b); // already reached through a

            Assert.That(_propagator.DirtyNodes.Count, Is.EqualTo(3));
        }

        [Test]
        public void ClearDirtyNodes_EmptiesTheSet()
        {
            _propagator.MarkTreeNodeDirty(Node());

            _propagator.ClearDirtyNodes();

            Assert.That(_propagator.DirtyNodes, Is.Empty);
        }

        // ---- MarkPropertyDirty ------------------------------------------------------------------

        [Test]
        public void MarkPropertyDirty_WithinSameEntity_MarksOnlyTheMatchingInstanceIndex()
        {
            Register(Turret, Level, TwoInstances);
            Register(Turret, Derived, TwoInstances);

            var levelNode = Node(Turret, Level);
            var derivedNode = Node(Turret, Derived);
            AddOutput(levelNode, derivedNode);

            _propagator.MarkPropertyDirty(PropertyId.New(Level, Turret, 0), levelNode, _store);

            Assert.That(Dirty(Level, Turret, 0), Is.True);
            Assert.That(Dirty(Level, Turret, 1), Is.False);
            Assert.That(Dirty(Derived, Turret, 0), Is.True);
            Assert.That(Dirty(Derived, Turret, 1), Is.False);
        }

        [Test]
        public void MarkPropertyDirty_FromGlobal_MarksEveryDownstreamInstance()
        {
            Register(Global, GlobalMultiplier, OneInstance);
            Register(Turret, Derived, TwoInstances);

            var globalNode = Node(Global, GlobalMultiplier);
            var derivedNode = Node(Turret, Derived);
            AddOutput(globalNode, derivedNode);

            _propagator.MarkPropertyDirty(PropertyId.New(GlobalMultiplier, Global, 0), globalNode, _store);

            Assert.That(Dirty(Derived, Turret, 0), Is.True);
            Assert.That(Dirty(Derived, Turret, 1), Is.True);
        }

        [Test]
        public void MarkPropertyDirty_CrossingThroughGlobal_FansOutToAllInstancesDownstream()
        {
            // TurretDps[0] -> TotalDps(global) -> Storage[*]
            Register(Turret, TurretDps, TwoInstances);
            Register(Global, TotalDps, OneInstance);
            Register(Container, Storage, TwoInstances);

            var dpsNode = Node(Turret, TurretDps);
            var totalNode = Node(Global, TotalDps);
            var storageNode = Node(Container, Storage);
            AddOutput(dpsNode, totalNode);
            AddOutput(totalNode, storageNode);

            _propagator.MarkPropertyDirty(PropertyId.New(TurretDps, Turret, 0), dpsNode, _store);

            // The single changed turret instance, the single global aggregate, and BOTH containers.
            Assert.That(Dirty(TurretDps, Turret, 0), Is.True);
            Assert.That(Dirty(TurretDps, Turret, 1), Is.False);
            Assert.That(Dirty(TotalDps, Global, 0), Is.True);
            Assert.That(Dirty(Storage, Container, 0), Is.True);
            Assert.That(Dirty(Storage, Container, 1), Is.True);
        }

        [Test]
        public void MarkPropertyDirty_NonGlobalToGlobal_MarksTheSingleGlobalInstance()
        {
            Register(Turret, TurretDps, TwoInstances);
            Register(Global, TotalDps, OneInstance);

            var dpsNode = Node(Turret, TurretDps);
            var totalNode = Node(Global, TotalDps);
            AddOutput(dpsNode, totalNode);

            _propagator.MarkPropertyDirty(PropertyId.New(TurretDps, Turret, 1), dpsNode, _store);

            Assert.That(Dirty(TotalDps, Global, 0), Is.True);
            // Only the changed sibling instance is dirty on the source entity.
            Assert.That(Dirty(TurretDps, Turret, 1), Is.True);
            Assert.That(Dirty(TurretDps, Turret, 0), Is.False);
        }

        [Test]
        public void MarkPropertyDirty_GlobalFanOut_DoesNotLeakOntoSiblingNonGlobalPaths()
        {
            // Level[0] -> GlobalMultiplier(global) -> Storage(container)   [global path -> all instances]
            // Level[0] -> Derived(turret) -> DownstreamDerived(turret)     [sibling path -> per instance]
            Register(Turret, Level, TwoInstances);
            Register(Global, GlobalMultiplier, OneInstance);
            Register(Container, Storage, TwoInstances);
            Register(Turret, Derived, TwoInstances);
            Register(Turret, DownstreamDerived, TwoInstances);

            var levelNode = Node(Turret, Level);
            var globalNode = Node(Global, GlobalMultiplier);
            var storageNode = Node(Container, Storage);
            var derivedNode = Node(Turret, Derived);
            var downstreamNode = Node(Turret, DownstreamDerived);
            AddOutput(levelNode, globalNode);
            AddOutput(globalNode, storageNode);
            AddOutput(levelNode, derivedNode);
            AddOutput(derivedNode, downstreamNode);

            _propagator.MarkPropertyDirty(PropertyId.New(Level, Turret, 0), levelNode, _store);

            // The global path fans out to every container instance.
            Assert.That(Dirty(Storage, Container, 0), Is.True);
            Assert.That(Dirty(Storage, Container, 1), Is.True);

            // The sibling path never touched global, so it stays instance-aligned even though it runs
            // deeper than the global node — the global fan-out must not leak across to it.
            Assert.That(Dirty(Derived, Turret, 0), Is.True);
            Assert.That(Dirty(Derived, Turret, 1), Is.False);
            Assert.That(Dirty(DownstreamDerived, Turret, 0), Is.True);
            Assert.That(Dirty(DownstreamDerived, Turret, 1), Is.False);
        }
    }
}
