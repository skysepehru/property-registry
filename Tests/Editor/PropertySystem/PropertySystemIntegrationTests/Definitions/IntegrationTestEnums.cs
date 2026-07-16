namespace skysepehru.Core.PropertyRegistry.Tests.Editor
{
    // Local, throwaway enums used by the whole suite. Their existence is also the proof that the
    // package works with ANY int-backed enums, not just the game's Entities/PropertyNames.

    internal enum IntegrationTestEntities
    {
        Global,
        Turret,
        Container,
    }


    //Simple property relationship for testing the property system.
    // LevelMultipliedByGlobalMultiplier (Per entity) = Entity Level (Per entity) * GlobalMultiplier (Global)
    // TurretDps (Per turret) = TurretStartingDps (Per turret) * LevelMultipliedByGlobalMultiplier (Per turret)
    // TotalOutputDps (Global) = Sum(TurretDps (Per turret))
    // ContainerStorage (Per storage) = TotalOutputDps (Global) * ContainerStartingBufferSeconds (Per storage) * LevelMultipliedByGlobalMultiplier (Per storage) / StorageCount (Global)
    internal enum IntegrationTestProperties
    {
        //Turret and Container base properties
        Level,

        //Turret base properties
        TurretStartingDps,

        //Container base properties
        ContainerStartingBufferSeconds,

        //Turret and Container derived properties
        LevelMultipliedByTwo,
        LevelMultipliedByGlobalMultiplier,

        //Turret derived properties
        TurretDps,

        //Container derived properties
        ContainerStorage,

        //Global base properties
        GlobalMultiplier,
        ContainerCount,

        //Global derived properties
        TotalOutputDps,

        //Global derived properties to test cyclic dependency handling
        XDerivedFromY,
        YDerivedFromX,
    }
}
