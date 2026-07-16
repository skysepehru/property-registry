using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertyTests
{
    public class PropertyTests
    {
        private enum Name { Health }
        private enum Entity { Turret }
        private const int NON_ZERO_DOUBLE = 5;
        private const int ANOTHER_NON_ZERO_DOUBLE = 10;


        [Test]
        public void Constructor_ExposesIdAndInitialValue()
        {
            var id = PropertyId.New(Name.Health, Entity.Turret, 0);
            var property = new Property(id, ANOTHER_NON_ZERO_DOUBLE);

            Assert.That(property.Id.Equals(id), Is.True);
            Assert.That(property.ValueReactive.Value, Is.EqualTo(ANOTHER_NON_ZERO_DOUBLE));
            Assert.That(property.ReadOnlyValueReactive.CurrentValue, Is.EqualTo(ANOTHER_NON_ZERO_DOUBLE));
        }

        [Test]
        public void Constructor_DefaultsToDirty()
        {
            var property = new Property(PropertyId.New(Name.Health, Entity.Turret, 0), 0);

            Assert.That(property.IsDirtied, Is.True);
        }

        [Test]
        public void Constructor_CanStartClean()
        {
            var property = new Property(PropertyId.New(Name.Health, Entity.Turret, 0), 0, isDirtied: false);

            Assert.That(property.IsDirtied, Is.False);
        }

        [Test]
        public void WritingValueReactive_IsVisibleThroughReadOnlyView()
        {
            var property = new Property(PropertyId.New(Name.Health, Entity.Turret, 0), NON_ZERO_DOUBLE);

            property.ValueReactive.Value = ANOTHER_NON_ZERO_DOUBLE;

            Assert.That(property.ReadOnlyValueReactive.CurrentValue, Is.EqualTo(ANOTHER_NON_ZERO_DOUBLE));
        }

        [Test]
        public void IsDirtied_IsMutable()
        {
            var property = new Property(PropertyId.New(Name.Health, Entity.Turret, 0), 0, isDirtied: false);

            property.IsDirtied = true;

            Assert.That(property.IsDirtied, Is.True);
        }
    }
}
