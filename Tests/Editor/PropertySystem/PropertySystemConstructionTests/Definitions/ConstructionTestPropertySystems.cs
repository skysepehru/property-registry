namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertySystemConstructionTests
{
    internal sealed class ConstructionTestPropertySystem
        : PropertySystem<ConstructionTestEntities, ConstructionTestProperties>
    {
        public ConstructionTestPropertySystem() : base(ConstructionTestEntities.Global)
        {
        }
    }

    internal sealed class ConstructionTestPropertySystemEntityInt64PropertyInt32
        : PropertySystem<ConstructionTestEntitiesInt64, ConstructionTestProperties>
    {
        public ConstructionTestPropertySystemEntityInt64PropertyInt32() : base(ConstructionTestEntitiesInt64.Global)
        {
        }
    }

    internal sealed class ConstructionTestPropertySystemEntityInt32PropertyInt64
        : PropertySystem<ConstructionTestEntities, ConstructionTestPropertiesInt64>
    {
        public ConstructionTestPropertySystemEntityInt32PropertyInt64() : base(ConstructionTestEntities.Global)
        {
        }
    }

    internal sealed class ConstructionTestPropertySystemEntityInt64PropertyInt64
        : PropertySystem<ConstructionTestEntitiesInt64, ConstructionTestPropertiesInt64>
    {
        public ConstructionTestPropertySystemEntityInt64PropertyInt64() : base(ConstructionTestEntitiesInt64.Global)
        {
        }
    }
}
