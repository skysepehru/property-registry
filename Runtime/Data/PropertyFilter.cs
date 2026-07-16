using System;

namespace skysepehru.Core.PropertyRegistry
{
    public struct PropertyFilter : IEquatable<PropertyFilter>
    {
        public int Name { get; }
        public int Entity { get; }

        private PropertyFilter(int name, int entity)
        {
            Name = name;
            Entity = entity;
        }

        public bool Equals(PropertyFilter other)
        {
            return Name == other.Name && Entity == other.Entity;
        }

        public override bool Equals(object obj)
        {
            return obj is PropertyFilter other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Name * 397) ^ Entity;
            }
        }

        public PropertyId ToPropertyId(int instanceId = 0)
        {
            return PropertyId.New(Name, Entity, instanceId);
        }

        public TName NameAs<TName>() where TName : unmanaged, Enum => EnumKey.ToEnum<TName>(Name);

        public TEntity EntityAs<TEntity>() where TEntity : unmanaged, Enum => EnumKey.ToEnum<TEntity>(Entity);

        internal static PropertyFilter New(int name, int entity)
        {
            return new PropertyFilter(name, entity);
        }

        public static PropertyFilter New<TName, TEntity>(TName name, TEntity entity)
            where TName : unmanaged, Enum
            where TEntity : unmanaged, Enum
        {
            return new PropertyFilter(EnumKey.ToInt(name), EnumKey.ToInt(entity));
        }

        public override string ToString()
        {
            return $"PropertyFilter(Name: {Name}, Entity: {Entity})";
        }
    }
}
