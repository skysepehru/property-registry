using System.Collections.Generic;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertyStoreTests
{
    public class PropertyStoreTests
    {
        private const int Global = 0;
        private const int Turret = 1;
        private const int Health = 10;
        private const int Damage = 11;
        private const double InitialValue = 7;

        private PropertyStore _store;

        [SetUp]
        public void SetUp()
        {
            _store = new PropertyStore(Global);
        }

        private static PropertyId Id(int entity, int name, int instance) => PropertyId.New(name, entity, instance);

        [Test]
        public void Register_ReturnsPropertyWithInitialValue_AndTracksIt()
        {
            var property = _store.Register(Id(Turret, Health, 0), InitialValue);

            Assert.That(property.ReadOnlyValueReactive.CurrentValue, Is.EqualTo(InitialValue));
            Assert.That(_store.Count, Is.EqualTo(1));
            Assert.That(_store.GetProperty(Id(Turret, Health, 0)), Is.SameAs(property));
        }

        [Test]
        public void Register_DuplicateId_Throws()
        {
            _store.Register(Id(Turret, Health, 0), 0);

            Assert.Catch(() => _store.Register(Id(Turret, Health, 0), 0));
        }

        [Test]
        public void Register_NonGlobalInstances_InOrder_Succeeds()
        {
            _store.Register(Id(Turret, Health, 0), 0);
            _store.Register(Id(Turret, Health, 1), 0);
            _store.Register(Id(Turret, Health, 2), 0);

            Assert.That(_store.GetEntityPropertyInstances(Turret, Health), Has.Count.EqualTo(3));
        }

        [Test]
        public void Register_NonGlobalInstance_OutOfOrder_Throws()
        {
            _store.Register(Id(Turret, Health, 0), 0);

            Assert.Catch(() => _store.Register(Id(Turret, Health, 2), 0));
        }

        [Test]
        public void Register_NonGlobalInstance_FirstIndexNotZero_Throws()
        {
            Assert.Catch(() => _store.Register(Id(Turret, Health, 1), 0));
        }

        [Test]
        public void Register_GlobalEntity_SecondInstance_Throws()
        {
            _store.Register(Id(Global, Health, 0), 0);

            Assert.Catch(() => _store.Register(Id(Global, Health, 1), 0));
        }

        [Test]
        public void Register_DistinctNamesOnSameEntity_AreIndependent()
        {
            _store.Register(Id(Turret, Health, 0), 0);
            _store.Register(Id(Turret, Damage, 0), 0);

            Assert.That(_store.GetEntityPropertyInstances(Turret, Health), Has.Count.EqualTo(1));
            Assert.That(_store.GetEntityPropertyInstances(Turret, Damage), Has.Count.EqualTo(1));
        }

        [Test]
        public void GetProperty_Missing_Throws()
        {
            Assert.Catch(() => _store.GetProperty(Id(Turret, Health, 0)));
        }

        [Test]
        public void GetEntityPropertyInstances_ReturnsInstancesInRegistrationOrder()
        {
            var first = _store.Register(Id(Turret, Health, 0), 0);
            var second = _store.Register(Id(Turret, Health, 1), 0);

            var instances = _store.GetEntityPropertyInstances(Turret, Health);

            Assert.That(instances[0], Is.SameAs(first));
            Assert.That(instances[1], Is.SameAs(second));
        }

        [Test]
        public void All_EnumeratesEveryRegisteredProperty()
        {
            _store.Register(Id(Turret, Health, 0), 0);
            _store.Register(Id(Global, Damage, 0), 0);

            var ids = new HashSet<PropertyId>();
            foreach (var kv in _store.All)
            {
                ids.Add(kv.Key);
            }

            Assert.That(ids, Has.Count.EqualTo(2));
            Assert.That(ids.Contains(Id(Turret, Health, 0)), Is.True);
            Assert.That(ids.Contains(Id(Global, Damage, 0)), Is.True);
        }
    }
}
