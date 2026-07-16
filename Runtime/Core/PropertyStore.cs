using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Owns every <see cref="Property"/> instance and enforces the registration invariants:
    /// no duplicate property, instances registered in incremental index order, and a single
    /// instance for the global entity. Pure storage + validation — no graph or evaluation logic.
    /// </summary>
    internal sealed class PropertyStore : IPropertyStore
    {
        private readonly int _globalEntity;
        private readonly Dictionary<PropertyId, Property> _properties = new();
        private readonly Dictionary<int, Dictionary<int, List<Property>>> _propertiesGrouped = new();

        public PropertyStore(int globalEntity)
        {
            _globalEntity = globalEntity;
        }

        public int Count => _properties.Count;

        public IEnumerable<KeyValuePair<PropertyId, Property>> All => _properties;

        public Property Register(PropertyId id, double initialValue)
        {
            if (_properties.ContainsKey(id))
            {
                throw new InvalidOperationException("Property is already registered.");
            }

            var existingInstanceCount = 0;
            if (_propertiesGrouped.TryGetValue(id.Entity, out var entityPropertiesDic)
                && entityPropertiesDic.TryGetValue(id.Name, out var entityPropertyInstancesDic))
            {
                existingInstanceCount = entityPropertyInstancesDic.Count;

                if (id.Entity == _globalEntity && existingInstanceCount > 0)
                {
                    throw new InvalidOperationException("Global entity can not have more than 1 instance.");
                }
            }
            
            if (id.Entity != _globalEntity && existingInstanceCount != id.InstanceIndex)
            {
                throw new InvalidOperationException("Entity instance properties must be registered in the correct incremental index order!");
            }

            entityPropertiesDic ??= new Dictionary<int, List<Property>>();
            _propertiesGrouped[id.Entity] = entityPropertiesDic;

            if (!entityPropertiesDic.TryGetValue(id.Name, out entityPropertyInstancesDic))
            {
                entityPropertyInstancesDic = new List<Property>();
                entityPropertiesDic[id.Name] = entityPropertyInstancesDic;
            }

            var property = new Property(id, initialValue);
            _properties[id] = property;
            entityPropertyInstancesDic.Add(property);
            return property;
        }

        public Property GetProperty(PropertyId id) => _properties[id];

        public List<Property> GetEntityPropertyInstances(int entity, int name) => _propertiesGrouped[entity][name];
    }
}
