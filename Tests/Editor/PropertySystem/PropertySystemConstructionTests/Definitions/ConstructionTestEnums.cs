namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertySystemConstructionTests
{
    internal enum ConstructionTestEntities
    {
        Global,
    }

    internal enum ConstructionTestProperties
    {
        BaseGlobal,
    }

    internal enum ConstructionTestEntitiesInt64 : long
    {
        Global = long.MaxValue,
    }

    internal enum ConstructionTestPropertiesInt64 : long
    {
        BaseGlobal = long.MaxValue,
    }
}
