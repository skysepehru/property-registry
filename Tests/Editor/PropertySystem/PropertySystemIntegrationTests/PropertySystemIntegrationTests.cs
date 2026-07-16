using System;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor
{
    /// <summary>
    /// End-to-end exercise of the whole pipeline (register -> connect -> dirty-propagate -> sort ->
    /// evaluate) over the full property graph documented in <see cref="IntegrationTestProperties"/>:
    ///
    ///   LevelMultipliedByTwo[e]              = Level[e] * 2
    ///   LevelMultipliedByGlobalMultiplier[e] = Level[e] * GlobalMultiplier
    ///   TurretDps[t]                         = TurretStartingDps[t] * LevelMultipliedByGlobalMultiplier[t]
    ///   TotalOutputDps                       = Sum(TurretDps[t])
    ///   ContainerStorage[c]                  = TotalOutputDps * ContainerStartingBufferSeconds[c]
    ///                                          * LevelMultipliedByGlobalMultiplier[c] / ContainerCount
    ///
    /// The graph has two turrets and two containers so per-instance isolation and global fan-out can
    /// both be asserted.
    /// </summary>
    public class PropertySystemIntegrationTests
    {
        private const double Tolerance = 1e-9;

        // Global bases.
        private const double GlobalMultiplier = 10;
        private const double ContainerCount = 2;

        // Turret bases (instances 0 and 1).
        private const double Turret0Level = 2;
        private const double Turret1Level = 3;
        private const double Turret0StartingDps = 5;
        private const double Turret1StartingDps = 7;

        // Container bases (instances 0 and 1).
        private const double Container0Level = 4;
        private const double Container1Level = 5;
        private const double Container0Buffer = 1.5;
        private const double Container1Buffer = 2.0;

        private IntegrationTestPropertySystem _system;

        [SetUp]
        public void SetUp()
        {
            _system = new IntegrationTestPropertySystem();
        }

        // ---- Full-graph value assertions -------------------------------------------------------

        [Test]
        public void InitialTick_ComputesEveryDerivedPropertyAcrossEveryInstance()
        {
            BuildFullGraphAndTick();

            // LevelMultipliedByTwo = Level * 2
            AssertValue(4, IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Turret, 0);
            AssertValue(6, IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Turret, 1);
            AssertValue(8, IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Container, 0);
            AssertValue(10, IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Container, 1);

            // LevelMultipliedByGlobalMultiplier = Level * GlobalMultiplier(10)
            AssertValue(20, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 0);
            AssertValue(30, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 1);
            AssertValue(40, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 0);
            AssertValue(50, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 1);

            // TurretDps = TurretStartingDps * LevelMultipliedByGlobalMultiplier
            AssertValue(100, IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 0);
            AssertValue(210, IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 1);

            // TotalOutputDps = sum of TurretDps
            AssertGlobal(310, IntegrationTestProperties.TotalOutputDps);

            // ContainerStorage = TotalOutputDps * Buffer * LevelMultipliedByGlobalMultiplier / ContainerCount(2)
            AssertValue(9300, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 0);
            AssertValue(15500, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 1);
        }

        [Test]
        public void RepeatedTickWithoutChanges_KeepsEveryValueStable()
        {
            BuildFullGraphAndTick();
            _system.Tick();
            _system.Tick();

            AssertValue(20, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 0);
            AssertGlobal(310, IntegrationTestProperties.TotalOutputDps);
            AssertValue(9300, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 0);
            AssertValue(15500, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 1);
        }
        
        [Test]
        public void GlobalMultiplierChange_RecomputesEveryMultiplierDependentButNotLevelDoubled()
        {
            BuildFullGraphAndTick();

            _system.SetBasePropertyValue(IntegrationTestProperties.GlobalMultiplier, IntegrationTestEntities.Global, 100);
            _system.Tick();

            // LevelMultipliedByTwo does not depend on the global multiplier -> unchanged.
            AssertValue(4, IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Turret, 0);
            AssertValue(10, IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Container, 1);

            // Everything on the multiplier chain rescales by 10x.
            AssertValue(200, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 0);
            AssertValue(300, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 1);
            AssertValue(400, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 0);
            AssertValue(500, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 1);

            AssertValue(1000, IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 0);
            AssertValue(2100, IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 1);
            AssertGlobal(3100, IntegrationTestProperties.TotalOutputDps);

            AssertValue(930000, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 0);
            AssertValue(1550000, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 1);
        }

        [Test]
        public void ContainerCountChange_RescalesOnlyContainerStorage()
        {
            BuildFullGraphAndTick();

            _system.SetBasePropertyValue(IntegrationTestProperties.ContainerCount, IntegrationTestEntities.Global, 4);
            _system.Tick();

            // Only the divisor changed; the rest of the graph is untouched.
            AssertValue(20, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 0);
            AssertValue(100, IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 0);
            AssertGlobal(310, IntegrationTestProperties.TotalOutputDps);

            AssertValue(4650, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 0);
            AssertValue(7750, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 1);
        }
        
        [Test]
        public void TurretLevelChange_UpdatesThatTurretChain_AndFansOutToAllContainersViaGlobal()
        {
            BuildFullGraphAndTick();

            _system.SetBasePropertyValue(IntegrationTestProperties.Level, IntegrationTestEntities.Turret, 12, 0);
            _system.Tick();

            // Changed turret's whole chain recomputes.
            AssertValue(24, IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Turret, 0);
            AssertValue(120, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 0);
            AssertValue(600, IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 0);

            // Sibling turret is untouched.
            AssertValue(6, IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Turret, 1);
            AssertValue(30, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 1);
            AssertValue(210, IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 1);

            // The global aggregate changes, so every container's storage is recomputed...
            AssertGlobal(810, IntegrationTestProperties.TotalOutputDps);
            AssertValue(24300, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 0);
            AssertValue(40500, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 1);

            // ...but the containers' own per-instance multipliers were not affected.
            AssertValue(40, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 0);
            AssertValue(50, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 1);
        }

        [Test]
        public void TurretStartingDpsChange_PropagatesToTotalsAndContainersOnly()
        {
            BuildFullGraphAndTick();

            _system.SetBasePropertyValue(IntegrationTestProperties.TurretStartingDps, IntegrationTestEntities.Turret, 15, 0);
            _system.Tick();

            // Multipliers don't depend on starting dps.
            AssertValue(20, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 0);

            AssertValue(300, IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 0);
            AssertValue(210, IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 1);
            AssertGlobal(510, IntegrationTestProperties.TotalOutputDps);

            AssertValue(15300, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 0);
            AssertValue(25500, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 1);
        }

        [Test]
        public void ContainerLevelChange_IsIsolatedToThatContainer()
        {
            BuildFullGraphAndTick();

            _system.SetBasePropertyValue(IntegrationTestProperties.Level, IntegrationTestEntities.Container, 14, 0);
            _system.Tick();

            // Only container 0's own derived values move.
            AssertValue(28, IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Container, 0);
            AssertValue(140, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 0);
            AssertValue(32550, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 0);

            // Sibling container, the turrets and the global aggregate are untouched.
            AssertValue(50, IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 1);
            AssertValue(15500, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 1);
            AssertGlobal(310, IntegrationTestProperties.TotalOutputDps);
        }

        [Test]
        public void ContainerBufferChange_IsIsolatedToThatContainer()
        {
            BuildFullGraphAndTick();

            _system.SetBasePropertyValue(IntegrationTestProperties.ContainerStartingBufferSeconds, IntegrationTestEntities.Container, 3.0, 0);
            _system.Tick();

            AssertValue(18600, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 0);
            AssertValue(15500, IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 1);
            AssertGlobal(310, IntegrationTestProperties.TotalOutputDps);
        }
        
        [Test]
        public void SetBasePropertyValue_OnDerivedProperty_Throws()
        {
            _system.RegisterBaseInstancedProperty(IntegrationTestProperties.Level, IntegrationTestEntities.Turret, 0, 1);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Turret, 0);
            _system.RegisterCalculator(new LevelMultipliedByTwoCalculator(IntegrationTestEntities.Turret));

            Assert.Catch(() =>
                _system.SetBasePropertyValue(IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Turret, 5));
        }

        [Test]
        public void RegisterCalculator_ClosingACyclicDependency_Throws()
        {
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.XDerivedFromY, IntegrationTestEntities.Turret, 0);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.YDerivedFromX, IntegrationTestEntities.Turret, 0);
            _system.RegisterCalculator<XFromYCalculator>();

            // Registering the calculator that closes the X<->Y loop must be rejected eagerly.
            Assert.Catch(() => _system.RegisterCalculator<YFromXCalculator>());
        }

        [Test]
        public void FirstTick_OnValidGraph_DoesNotThrow()
        {
            BuildFullGraph();

            Assert.DoesNotThrow(() => _system.Tick());
        }

        [Test]
        public void SetBasePropertyValue_OnUnregisteredProperty_Throws()
        {
            Assert.Catch(() =>
                _system.SetBasePropertyValue(IntegrationTestProperties.Level, IntegrationTestEntities.Turret, 5));
        }

        private void BuildFullGraphAndTick()
        {
            BuildFullGraph();
            _system.Tick();
        }

        private void BuildFullGraph()
        {
            // Global bases.
            _system.RegisterBaseGlobalProperty(IntegrationTestProperties.GlobalMultiplier, GlobalMultiplier);
            _system.RegisterBaseGlobalProperty(IntegrationTestProperties.ContainerCount, ContainerCount);

            // Turret bases.
            _system.RegisterBaseInstancedProperty(IntegrationTestProperties.Level, IntegrationTestEntities.Turret, 0, Turret0Level);
            _system.RegisterBaseInstancedProperty(IntegrationTestProperties.Level, IntegrationTestEntities.Turret, 1, Turret1Level);
            _system.RegisterBaseInstancedProperty(IntegrationTestProperties.TurretStartingDps, IntegrationTestEntities.Turret, 0, Turret0StartingDps);
            _system.RegisterBaseInstancedProperty(IntegrationTestProperties.TurretStartingDps, IntegrationTestEntities.Turret, 1, Turret1StartingDps);

            // Container bases.
            _system.RegisterBaseInstancedProperty(IntegrationTestProperties.Level, IntegrationTestEntities.Container, 0, Container0Level);
            _system.RegisterBaseInstancedProperty(IntegrationTestProperties.Level, IntegrationTestEntities.Container, 1, Container1Level);
            _system.RegisterBaseInstancedProperty(IntegrationTestProperties.ContainerStartingBufferSeconds, IntegrationTestEntities.Container, 0, Container0Buffer);
            _system.RegisterBaseInstancedProperty(IntegrationTestProperties.ContainerStartingBufferSeconds, IntegrationTestEntities.Container, 1, Container1Buffer);

            // Derived properties.
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Turret, 0);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Turret, 1);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Container, 0);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.LevelMultipliedByTwo, IntegrationTestEntities.Container, 1);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 0);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret, 1);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 0);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container, 1);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 0);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret, 1);
            _system.RegisterDerivedGlobalProperty(IntegrationTestProperties.TotalOutputDps);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 0);
            _system.RegisterDerivedInstancedProperty(IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container, 1);

            // Calculators. Per-entity ones are registered once per target entity.
            _system.RegisterCalculator(new LevelMultipliedByTwoCalculator(IntegrationTestEntities.Turret));
            _system.RegisterCalculator(new LevelMultipliedByTwoCalculator(IntegrationTestEntities.Container));
            _system.RegisterCalculator(new LevelMultipliedByGlobalMultiplierCalculator(IntegrationTestEntities.Turret));
            _system.RegisterCalculator(new LevelMultipliedByGlobalMultiplierCalculator(IntegrationTestEntities.Container));
            _system.RegisterCalculator<TurretDpsCalculator>();
            _system.RegisterCalculator<TotalOutputDpsCalculator>();
            _system.RegisterCalculator<ContainerStorageCalculator>();
        }

        private void AssertValue(double expected, IntegrationTestProperties name, IntegrationTestEntities entity, int instanceIndex)
        {
            Assert.That(_system[name, entity, instanceIndex].CurrentValue, Is.EqualTo(expected).Within(Tolerance),
                $"{entity}.{name}[{instanceIndex}]");
        }

        private void AssertGlobal(double expected, IntegrationTestProperties name)
        {
            Assert.That(_system[name, IntegrationTestEntities.Global, 0].CurrentValue, Is.EqualTo(expected).Within(Tolerance),
                $"Global.{name}");
        }
    }
}
