using System.Collections.Generic;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertyIdTests
{
    public class PropertyIdTests
    {
        private enum Name { Health, Damage }
        private enum Entity { Global, Turret }
        private const int NON_ZERO_DOUBLE = 5;

        [Test]
        public void New_StoresEnumKeysAndInstanceIndex()
        {
            var id = PropertyId.New(Name.Damage, Entity.Turret, NON_ZERO_DOUBLE);

            Assert.That(id.Name, Is.EqualTo((int)Name.Damage));
            Assert.That(id.Entity, Is.EqualTo((int)Entity.Turret));
            Assert.That(id.InstanceIndex, Is.EqualTo(NON_ZERO_DOUBLE));
        }

        [Test]
        public void Equals_IsTrue_ForIdenticalComponents()
        {
            var a = PropertyId.New(Name.Health, Entity.Turret, NON_ZERO_DOUBLE);
            var b = PropertyId.New(Name.Health, Entity.Turret, NON_ZERO_DOUBLE);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Equals_IsFalse_WhenAnyComponentDiffers()
        {
            var baseId = PropertyId.New(Name.Health, Entity.Turret, NON_ZERO_DOUBLE);

            Assert.That(baseId.Equals(PropertyId.New(Name.Damage, Entity.Turret, NON_ZERO_DOUBLE)), Is.False);
            Assert.That(baseId.Equals(PropertyId.New(Name.Health, Entity.Global, NON_ZERO_DOUBLE)), Is.False);
            Assert.That(baseId.Equals(PropertyId.New(Name.Health, Entity.Turret, NON_ZERO_DOUBLE + 1)), Is.False);
        }

        [Test]
        public void Equals_Object_HandlesWrongTypeAndNull()
        {
            var id = PropertyId.New(Name.Health, Entity.Turret, 0);

            // ReSharper disable once SuspiciousTypeConversion.Global
            Assert.That(id.Equals("not an id"), Is.False);
            Assert.That(id.Equals(null), Is.False);
        }

        [Test]
        public void UsableAsDictionaryKey()
        {
            var dict = new Dictionary<PropertyId, int>
            {
                [PropertyId.New(Name.Health, Entity.Turret, 0)] = NON_ZERO_DOUBLE,
            };

            Assert.That(dict[PropertyId.New(Name.Health, Entity.Turret, 0)], Is.EqualTo(NON_ZERO_DOUBLE));
            Assert.That(dict.ContainsKey(PropertyId.New(Name.Health, Entity.Turret, 1)), Is.False);
        }

        [Test]
        public void ToPropertyFilter_KeepsNameAndEntity_DropsInstance()
        {
            var filter = PropertyId.New(Name.Damage, Entity.Turret, NON_ZERO_DOUBLE).ToPropertyFilter();

            Assert.That(filter.Name, Is.EqualTo((int)Name.Damage));
            Assert.That(filter.Entity, Is.EqualTo((int)Entity.Turret));
        }

        [Test]
        public void NameAs_And_EntityAs_RoundTripToTheOriginalEnums()
        {
            var id = PropertyId.New(Name.Damage, Entity.Turret, 0);

            Assert.That(id.NameAs<Name>(), Is.EqualTo(Name.Damage));
            Assert.That(id.EntityAs<Entity>(), Is.EqualTo(Entity.Turret));
        }
    }
}
