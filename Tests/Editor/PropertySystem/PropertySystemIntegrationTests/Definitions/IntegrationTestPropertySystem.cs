namespace skysepehru.Core.PropertyRegistry.Tests.Editor
{
    internal class IntegrationTestPropertySystem : PropertySystem<IntegrationTestEntities, IntegrationTestProperties>
    {
        public IntegrationTestPropertySystem() : base(IntegrationTestEntities.Global)
        {
        }
    }
}
