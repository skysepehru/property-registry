using System;

namespace skysepehru.Core.PropertyRegistry
{
    public readonly struct PropertyId : IEquatable<PropertyId>
    {
        public int Name { get; }
        public int Entity { get; }
        public int InstanceIndex { get; }

        private PropertyId(int name, int entity, int instanceIndex)
        {
            Name = name;
            Entity = entity;
            InstanceIndex = instanceIndex;
        }

        public bool Equals(PropertyId other)
        {
            return Name == other.Name && Entity == other.Entity && InstanceIndex == other.InstanceIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is PropertyId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name;
                hashCode = (hashCode * 397) ^ Entity;
                hashCode = (hashCode * 397) ^ InstanceIndex;
                return hashCode;
            }
        }

        public PropertyFilter ToPropertyFilter()
        {
            return PropertyFilter.New(Name, Entity);
        }

        public TName NameAs<TName>() where TName : unmanaged, Enum => EnumKey.ToEnum<TName>(Name);

        public TEntity EntityAs<TEntity>() where TEntity : unmanaged, Enum => EnumKey.ToEnum<TEntity>(Entity);
        
        internal static PropertyId New(int name, int entity, int instanceIndex)
        {
            return new PropertyId(name, entity, instanceIndex);
        }

        public static PropertyId New<TName, TEntity>(TName name, TEntity entity, int instanceIndex)
            where TName : unmanaged, Enum
            where TEntity : unmanaged, Enum
        {
            return new PropertyId(EnumKey.ToInt(name), EnumKey.ToInt(entity), instanceIndex);
        }
    }
}
