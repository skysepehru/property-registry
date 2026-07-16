namespace skysepehru.Core.PropertyRegistry.Tests.Editor
{
    // Calculators that model the property graph documented in IntegrationTestEnums:
    //
    //   LevelMultipliedByTwo[e]              = Level[e] * 2
    //   LevelMultipliedByGlobalMultiplier[e] = Level[e] * GlobalMultiplier(global)
    //   TurretDps[t]                         = TurretStartingDps[t] * LevelMultipliedByGlobalMultiplier[t]
    //   TotalOutputDps(global)               = Sum(TurretDps[t])
    //   ContainerStorage[c]                  = TotalOutputDps(global) * ContainerStartingBufferSeconds[c]
    //                                          * LevelMultipliedByGlobalMultiplier[c] / ContainerCount(global)
    //
    // Properties that exist on more than one entity (LevelMultipliedByTwo,
    // LevelMultipliedByGlobalMultiplier) are computed by entity-parameterised calculators: the target
    // entity is passed in, so the same class serves Turret and Container. Register those with the
    // non-generic RegisterCalculator(IPropertyCalculator) overload, once per entity.

    /// <summary>LevelMultipliedByTwo@Entity[i] = Level@Entity[i] * 2.</summary>
    internal sealed class LevelMultipliedByTwoCalculator : IPropertyCalculator
    {
        private readonly IntegrationTestEntities _entity;
        private InputHandle _level;

        public LevelMultipliedByTwoCalculator(IntegrationTestEntities entity) => _entity = entity;

        public void Declare(CalculatorBuilder builder)
        {
            _level = builder.AddInput(IntegrationTestProperties.Level, _entity);
            builder.SetOutput(IntegrationTestProperties.LevelMultipliedByTwo, _entity);
        }

        public void Calculate(in CalculationContext context)
        {
            var levels = context.Inputs(_level);
            var outputs = context.Outputs;
            for (int i = 0; i < outputs.Count; i++)
            {
                if (!outputs.IsDirty(i))
                {
                    continue;
                }

                outputs.Set(i, levels[i] * 2);
            }
        }
    }

    /// <summary>
    /// LevelMultipliedByGlobalMultiplier@Entity[i] = Level@Entity[i] * GlobalMultiplier@Global.
    /// A single global input feeding every per-instance output — exercises the global dirty fan-out.
    /// </summary>
    internal sealed class LevelMultipliedByGlobalMultiplierCalculator : IPropertyCalculator
    {
        private readonly IntegrationTestEntities _entity;
        private InputHandle _level;
        private InputHandle _multiplier;

        public LevelMultipliedByGlobalMultiplierCalculator(IntegrationTestEntities entity) => _entity = entity;

        public void Declare(CalculatorBuilder builder)
        {
            _level = builder.AddInput(IntegrationTestProperties.Level, _entity);
            _multiplier = builder.AddInput(IntegrationTestProperties.GlobalMultiplier, IntegrationTestEntities.Global);
            builder.SetOutput(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, _entity);
        }

        public void Calculate(in CalculationContext context)
        {
            var levels = context.Inputs(_level);
            var multiplier = context.Value(_multiplier);
            var outputs = context.Outputs;
            for (int i = 0; i < outputs.Count; i++)
            {
                if (!outputs.IsDirty(i))
                {
                    continue;
                }

                outputs.Set(i, levels[i] * multiplier);
            }
        }
    }

    /// <summary>TurretDps@Turret[i] = TurretStartingDps@Turret[i] * LevelMultipliedByGlobalMultiplier@Turret[i].</summary>
    internal sealed class TurretDpsCalculator : IPropertyCalculator
    {
        private InputHandle _startingDps;
        private InputHandle _scaled;

        public void Declare(CalculatorBuilder builder)
        {
            _startingDps = builder.AddInput(IntegrationTestProperties.TurretStartingDps, IntegrationTestEntities.Turret);
            _scaled = builder.AddInput(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret);
            builder.SetOutput(IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret);
        }

        public void Calculate(in CalculationContext context)
        {
            var startingDps = context.Inputs(_startingDps);
            var scaled = context.Inputs(_scaled);
            var outputs = context.Outputs;
            for (int i = 0; i < outputs.Count; i++)
            {
                if (!outputs.IsDirty(i))
                {
                    continue;
                }

                outputs.Set(i, startingDps[i] * scaled[i]);
            }
        }
    }

    /// <summary>
    /// TotalOutputDps@Global = Sum(TurretDps@Turret). Many per-turret inputs collapsing into the
    /// single global output — exercises an entity-to-global aggregation.
    /// </summary>
    internal sealed class TotalOutputDpsCalculator : IPropertyCalculator
    {
        private InputHandle _turretDps;

        public void Declare(CalculatorBuilder builder)
        {
            _turretDps = builder.AddInput(IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret);
            builder.SetOutput(IntegrationTestProperties.TotalOutputDps, IntegrationTestEntities.Global);
        }

        public void Calculate(in CalculationContext context)
        {
            var outputs = context.Outputs;
            if (!outputs.IsDirty(0))
            {
                return;
            }

            var turretDps = context.Inputs(_turretDps);
            double sum = 0;
            for (int i = 0; i < turretDps.Count; i++)
            {
                sum += turretDps[i];
            }

            outputs.Set(0, sum);
        }
    }

    /// <summary>
    /// ContainerStorage@Container[i] =
    ///     TotalOutputDps@Global * ContainerStartingBufferSeconds@Container[i]
    ///     * LevelMultipliedByGlobalMultiplier@Container[i] / ContainerCount@Global.
    /// Mixes global and per-instance inputs into a per-instance output — the deepest node in the graph.
    /// </summary>
    internal sealed class ContainerStorageCalculator : IPropertyCalculator
    {
        private InputHandle _totalOutputDps;
        private InputHandle _bufferSeconds;
        private InputHandle _scaled;
        private InputHandle _containerCount;

        public void Declare(CalculatorBuilder builder)
        {
            _totalOutputDps = builder.AddInput(IntegrationTestProperties.TotalOutputDps, IntegrationTestEntities.Global);
            _bufferSeconds = builder.AddInput(IntegrationTestProperties.ContainerStartingBufferSeconds, IntegrationTestEntities.Container);
            _scaled = builder.AddInput(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container);
            _containerCount = builder.AddInput(IntegrationTestProperties.ContainerCount, IntegrationTestEntities.Global);
            builder.SetOutput(IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container);
        }

        public void Calculate(in CalculationContext context)
        {
            var totalOutputDps = context.Value(_totalOutputDps);
            var bufferSeconds = context.Inputs(_bufferSeconds);
            var scaled = context.Inputs(_scaled);
            var containerCount = context.Value(_containerCount);
            var outputs = context.Outputs;

            for (int i = 0; i < outputs.Count; i++)
            {
                if (!outputs.IsDirty(i))
                {
                    continue;
                }

                outputs.Set(i, totalOutputDps * bufferSeconds[i] * scaled[i] / containerCount);
            }
        }
    }

    // XDerivedFromY depends on YDerivedFromX and vice versa -> a cycle rejected when the second
    // calculator is registered.
    internal sealed class XFromYCalculator : IPropertyCalculator
    {
        public void Declare(CalculatorBuilder builder)
        {
            builder.AddInput(IntegrationTestProperties.YDerivedFromX, IntegrationTestEntities.Turret);
            builder.SetOutput(IntegrationTestProperties.XDerivedFromY, IntegrationTestEntities.Turret);
        }

        public void Calculate(in CalculationContext context)
        {
            // Never reached: registering the loop-closing calculator throws before any tick runs.
        }
    }

    internal sealed class YFromXCalculator : IPropertyCalculator
    {
        public void Declare(CalculatorBuilder builder)
        {
            builder.AddInput(IntegrationTestProperties.XDerivedFromY, IntegrationTestEntities.Turret);
            builder.SetOutput(IntegrationTestProperties.YDerivedFromX, IntegrationTestEntities.Turret);
        }

        public void Calculate(in CalculationContext context)
        {
        }
    }
}
