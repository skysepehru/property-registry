using System;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Serializable address of a single entity instance (its entity kind + instance index).
    /// Generic over the entity enum so the package stays agnostic of any concrete enum while
    /// remaining usable as a Unity-serialized field when closed (e.g. EntityInstanceId&lt;Entities&gt;).
    /// </summary>
    [Serializable]
    public struct EntityInstanceId<TEntity> : IEquatable<EntityInstanceId<TEntity>> where TEntity : unmanaged, Enum
    {
        public TEntity Entity;
        public int Index;

        public EntityInstanceId(TEntity entity, int index)
        {
            Entity = entity;
            Index = index;
        }

        public bool Equals(EntityInstanceId<TEntity> other)
        {
            return Entity.Equals(other.Entity) && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityInstanceId<TEntity> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Entity.GetHashCode() * 397) ^ Index;
            }
        }
    }
}
