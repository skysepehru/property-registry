using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.EntityInstanceIdTests
{
    public class EntityInstanceIdTests
    {
        private enum Entity { Global, Turret, Container }

        private const int Index = 2;
        private const int ReassignedIndex = 5;
        private const int EqualIndex = 3;
        private const int DifferentIndex = 4;

        [Test]
        public void Constructor_SetsEntityAndIndex()
        {
            var id = new EntityInstanceId<Entity>(Entity.Turret, Index);

            Assert.That(id.Entity, Is.EqualTo(Entity.Turret));
            Assert.That(id.Index, Is.EqualTo(Index));
        }

        [Test]
        public void Default_HasIndexZeroAndFirstEnumValue()
        {
            var id = default(EntityInstanceId<Entity>);

            Assert.That(id.Entity, Is.EqualTo(default(Entity)));
            Assert.That(id.Index, Is.EqualTo(0));
        }

        [Test]
        public void Fields_AreMutable()
        {
            var id = new EntityInstanceId<Entity>(Entity.Turret, Index)
            {
                Entity = Entity.Container,
                Index = ReassignedIndex,
            };

            Assert.That(id.Entity, Is.EqualTo(Entity.Container));
            Assert.That(id.Index, Is.EqualTo(ReassignedIndex));
        }

        [Test]
        public void ValueEquality_ComparesEntityAndIndex()
        {
            var a = new EntityInstanceId<Entity>(Entity.Turret, EqualIndex);
            var b = new EntityInstanceId<Entity>(Entity.Turret, EqualIndex);
            var different = new EntityInstanceId<Entity>(Entity.Turret, DifferentIndex);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.Equals(different), Is.False);
        }
    }
}
