using System.Collections.Generic;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertyFilterTests
{
    public class PropertyFilterTests
    {
        private enum Entity { Global, Turret }
        private enum Name { Health, Damage, TotalDps }

        private const int NON_ZERO_DOUBLE = 5;

        [Test]
        public void New_StoresEnumKeys()
        {
            var filter = PropertyFilter.New(Name.Damage, Entity.Turret);

            Assert.That(filter.Name, Is.EqualTo((int)Name.Damage));
            Assert.That(filter.Entity, Is.EqualTo((int)Entity.Turret));
        }

        [Test]
        public void Equals_IsTrue_ForIdenticalComponents()
        {
            var a = PropertyFilter.New(Name.Health, Entity.Turret);
            var b = PropertyFilter.New(Name.Health, Entity.Turret);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Equals_WhenAnyComponentDiffers_IsFalse()
        {
            var baseFilter = PropertyFilter.New(Name.Health, Entity.Turret);

            Assert.That(baseFilter.Equals(PropertyFilter.New(Name.Damage, Entity.Turret)), Is.False);
            Assert.That(baseFilter.Equals(PropertyFilter.New(Name.Health, Entity.Global)), Is.False);
        }

        [Test]
        public void Equals_Object_HandlesWrongTypeAndNull()
        {
            var filter = PropertyFilter.New(Name.Health, Entity.Turret);
            
            // ReSharper disable once SuspiciousTypeConversion.Global
            Assert.That(filter.Equals("not a filter"), Is.False);
            Assert.That(filter.Equals(null), Is.False);
        }

        [Test]
        public void UsableAsDictionaryKey()
        {
            var dict = new Dictionary<PropertyFilter, int>
            {
                [PropertyFilter.New(Name.Health, Entity.Turret)] = NON_ZERO_DOUBLE,
            };

            Assert.That(dict[PropertyFilter.New(Name.Health, Entity.Turret)], Is.EqualTo(NON_ZERO_DOUBLE));
            Assert.That(dict.ContainsKey(PropertyFilter.New(Name.Damage, Entity.Turret)), Is.False);
        }

        [Test]
        public void ToPropertyId_AddsInstanceIndex_KeepsNameAndEntity()
        {
            var id = PropertyFilter.New(Name.Health, Entity.Turret).ToPropertyId(NON_ZERO_DOUBLE);

            Assert.That(id.Name, Is.EqualTo((int)Name.Health));
            Assert.That(id.Entity, Is.EqualTo((int)Entity.Turret));
            Assert.That(id.InstanceIndex, Is.EqualTo(NON_ZERO_DOUBLE));
        }

        [Test]
        public void ToPropertyId_DefaultsInstanceIndexToZero()
        {
            var id = PropertyFilter.New(Name.Health, Entity.Turret).ToPropertyId();

            Assert.That(id.InstanceIndex, Is.EqualTo(0));
        }

        [Test]
        public void NameAs_And_EntityAs_RoundTripToTheOriginalEnums()
        {
            var filter = PropertyFilter.New(Name.Damage, Entity.Turret);

            Assert.That(filter.NameAs<Name>(), Is.EqualTo(Name.Damage));
            Assert.That(filter.EntityAs<Entity>(), Is.EqualTo(Entity.Turret));
        }

        [Test]
        public void Roundtrips_ThroughPropertyId()
        {
            var original = PropertyFilter.New(Name.Damage, Entity.Turret);

            var roundTripped = original.ToPropertyId(NON_ZERO_DOUBLE).ToPropertyFilter();

            Assert.That(roundTripped.Equals(original), Is.True);
        }
    }
}
