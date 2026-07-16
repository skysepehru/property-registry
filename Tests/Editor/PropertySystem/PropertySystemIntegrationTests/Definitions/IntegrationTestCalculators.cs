using System;
using System.Collections.Generic;

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

        public LevelMultipliedByTwoCalculator(IntegrationTestEntities entity) => _entity = entity;

        public int GetInputProperties(Span<PropertyFilter> buffer)
        {
            buffer[0] = PropertyFilter.New(IntegrationTestProperties.Level, _entity);
            return 1;
        }

        public PropertyFilter GetOutputProperty()
            => PropertyFilter.New(IntegrationTestProperties.LevelMultipliedByTwo, _entity);

        public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputs, List<Property> outputs)
        {
            var levels = inputs[0];
            for (int i = 0; i < outputs.Count; i++)
            {
                if (!outputs[i].IsDirtied)
                {
                    continue;
                }

                outputs[i].ValueReactive.Value = levels[i].ReadOnlyValueReactive.CurrentValue * 2;
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

        public LevelMultipliedByGlobalMultiplierCalculator(IntegrationTestEntities entity) => _entity = entity;

        public int GetInputProperties(Span<PropertyFilter> buffer)
        {
            buffer[0] = PropertyFilter.New(IntegrationTestProperties.Level, _entity);
            buffer[1] = PropertyFilter.New(IntegrationTestProperties.GlobalMultiplier, IntegrationTestEntities.Global);
            return 2;
        }

        public PropertyFilter GetOutputProperty()
            => PropertyFilter.New(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, _entity);

        public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputs, List<Property> outputs)
        {
            var levels = inputs[0];
            var multiplier = inputs[1][0].ReadOnlyValueReactive.CurrentValue;
            for (int i = 0; i < outputs.Count; i++)
            {
                if (!outputs[i].IsDirtied)
                {
                    continue;
                }

                outputs[i].ValueReactive.Value = levels[i].ReadOnlyValueReactive.CurrentValue * multiplier;
            }
        }
    }

    /// <summary>TurretDps@Turret[i] = TurretStartingDps@Turret[i] * LevelMultipliedByGlobalMultiplier@Turret[i].</summary>
    internal sealed class TurretDpsCalculator : IPropertyCalculator
    {
        public int GetInputProperties(Span<PropertyFilter> buffer)
        {
            buffer[0] = PropertyFilter.New(IntegrationTestProperties.TurretStartingDps, IntegrationTestEntities.Turret);
            buffer[1] = PropertyFilter.New(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Turret);
            return 2;
        }

        public PropertyFilter GetOutputProperty()
            => PropertyFilter.New(IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret);

        public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputs, List<Property> outputs)
        {
            var startingDps = inputs[0];
            var scaled = inputs[1];
            for (int i = 0; i < outputs.Count; i++)
            {
                if (!outputs[i].IsDirtied)
                {
                    continue;
                }

                outputs[i].ValueReactive.Value =
                    startingDps[i].ReadOnlyValueReactive.CurrentValue * scaled[i].ReadOnlyValueReactive.CurrentValue;
            }
        }
    }

    /// <summary>
    /// TotalOutputDps@Global = Sum(TurretDps@Turret). Many per-turret inputs collapsing into the
    /// single global output — exercises an entity-to-global aggregation.
    /// </summary>
    internal sealed class TotalOutputDpsCalculator : IPropertyCalculator
    {
        public int GetInputProperties(Span<PropertyFilter> buffer)
        {
            buffer[0] = PropertyFilter.New(IntegrationTestProperties.TurretDps, IntegrationTestEntities.Turret);
            return 1;
        }

        public PropertyFilter GetOutputProperty()
            => PropertyFilter.New(IntegrationTestProperties.TotalOutputDps, IntegrationTestEntities.Global);

        public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputs, List<Property> outputs)
        {
            if (!outputs[0].IsDirtied)
            {
                return;
            }

            var turretDps = inputs[0];
            double sum = 0;
            for (int i = 0; i < turretDps.Count; i++)
            {
                sum += turretDps[i].ReadOnlyValueReactive.CurrentValue;
            }

            outputs[0].ValueReactive.Value = sum;
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
        public int GetInputProperties(Span<PropertyFilter> buffer)
        {
            buffer[0] = PropertyFilter.New(IntegrationTestProperties.TotalOutputDps, IntegrationTestEntities.Global);
            buffer[1] = PropertyFilter.New(IntegrationTestProperties.ContainerStartingBufferSeconds, IntegrationTestEntities.Container);
            buffer[2] = PropertyFilter.New(IntegrationTestProperties.LevelMultipliedByGlobalMultiplier, IntegrationTestEntities.Container);
            buffer[3] = PropertyFilter.New(IntegrationTestProperties.ContainerCount, IntegrationTestEntities.Global);
            return 4;
        }

        public PropertyFilter GetOutputProperty()
            => PropertyFilter.New(IntegrationTestProperties.ContainerStorage, IntegrationTestEntities.Container);

        public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputs, List<Property> outputs)
        {
            var totalOutputDps = inputs[0][0].ReadOnlyValueReactive.CurrentValue;
            var bufferSeconds = inputs[1];
            var scaled = inputs[2];
            var containerCount = inputs[3][0].ReadOnlyValueReactive.CurrentValue;

            for (int i = 0; i < outputs.Count; i++)
            {
                if (!outputs[i].IsDirtied)
                {
                    continue;
                }

                outputs[i].ValueReactive.Value =
                    totalOutputDps
                    * bufferSeconds[i].ReadOnlyValueReactive.CurrentValue
                    * scaled[i].ReadOnlyValueReactive.CurrentValue
                    / containerCount;
            }
        }
    }

    // XDerivedFromY depends on YDerivedFromX and vice versa -> a cycle rejected when the second
    // calculator is registered.
    internal sealed class XFromYCalculator : IPropertyCalculator
    {
        public int GetInputProperties(Span<PropertyFilter> buffer)
        {
            buffer[0] = PropertyFilter.New(IntegrationTestProperties.YDerivedFromX, IntegrationTestEntities.Turret);
            return 1;
        }

        public PropertyFilter GetOutputProperty()
            => PropertyFilter.New(IntegrationTestProperties.XDerivedFromY, IntegrationTestEntities.Turret);

        public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputs, List<Property> outputs)
        {
            // Never reached: registering the loop-closing calculator throws before any tick runs.
        }
    }

    internal sealed class YFromXCalculator : IPropertyCalculator
    {
        public int GetInputProperties(Span<PropertyFilter> buffer)
        {
            buffer[0] = PropertyFilter.New(IntegrationTestProperties.XDerivedFromY, IntegrationTestEntities.Turret);
            return 1;
        }

        public PropertyFilter GetOutputProperty()
            => PropertyFilter.New(IntegrationTestProperties.YDerivedFromX, IntegrationTestEntities.Turret);

        public void Calculate(IReadOnlyList<IReadOnlyList<IReadOnlyProperty>> inputs, List<Property> outputs)
        {
        }
    }
}
