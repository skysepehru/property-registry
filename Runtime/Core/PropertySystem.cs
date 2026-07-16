using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using R3;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Orchestrates the property registry: it validates registration intent and wires the modules
    /// that do the work — storage (<see cref="PropertyStore"/>), the dependency graph
    /// (<see cref="PropertyDependencyGraph"/>), dirty propagation (<see cref="DirtyPropagator"/>),
    /// ordering (<see cref="DirtySubgraphSorter"/>) and evaluation (<see cref="CalculatorEvaluator"/>).
    /// </summary>
    public abstract class PropertySystem<TEntity, TProperty>
        where TEntity : unmanaged, Enum
        where TProperty : unmanaged, Enum
    {
        private readonly int _globalEntity;

        private readonly IPropertyStore _store;
        private readonly IPropertyDependencyGraph _graph;
        private readonly IDirtyPropagator _propagator;
        private readonly IDirtySubgraphSorter _sorter;
        private readonly ICalculatorEvaluator _evaluator;

        protected PropertySystem(TEntity globalEntity)
            : this(
                globalEntity,
                new PropertyStore(EnumKey.ToInt(globalEntity)),
                new PropertyDependencyGraph(),
                new DirtyPropagator(EnumKey.ToInt(globalEntity)),
                new DirtySubgraphSorter(),
                new CalculatorEvaluator())
        {
        }

        /// <summary>
        /// Test seam: builds the orchestrator over injected subsystems so its wiring and validation can
        /// be exercised in isolation. Production code uses the <see cref="PropertySystem(TEntity)"/>
        /// overload, which supplies the real subsystems.
        /// </summary>
        internal PropertySystem(
            TEntity globalEntity,
            IPropertyStore store,
            IPropertyDependencyGraph graph,
            IDirtyPropagator propagator,
            IDirtySubgraphSorter sorter,
            ICalculatorEvaluator evaluator)
        {
            EnumKey.EnsureInt32Backed<TEntity>();
            EnumKey.EnsureInt32Backed<TProperty>();

            _globalEntity = EnumKey.ToInt(globalEntity);

            _store = store;
            _graph = graph;
            _propagator = propagator;
            _sorter = sorter;
            _evaluator = evaluator;
        }

        public ReadOnlyReactiveProperty<double> RegisterBaseInstancedProperty(TProperty name, TEntity entity, int instanceIndex, double initialValue)
        {
            if (EnumKey.ToInt(entity) == _globalEntity)
            {
                throw new InvalidEnumArgumentException($"Registering a global property through {nameof(RegisterBaseInstancedProperty)} is not allowed.");
            }

            return RegisterProperty(PropertyId.New(name, entity, instanceIndex), true, initialValue);
        }

        public ReadOnlyReactiveProperty<double> RegisterBaseGlobalProperty(TProperty name, double initialValue)
        {
            return RegisterProperty(PropertyId.New(name, EnumKey.ToEnum<TEntity>(_globalEntity), 0), true, initialValue);
        }

        public ReadOnlyReactiveProperty<double> RegisterDerivedInstancedProperty(TProperty name, TEntity entity, int instanceIndex)
        {
            if (EnumKey.ToInt(entity) == _globalEntity)
            {
                throw new InvalidEnumArgumentException($"Registering a global property through {nameof(RegisterDerivedInstancedProperty)} is not allowed.");
            }

            return RegisterProperty(PropertyId.New(name, entity, instanceIndex), false);
        }

        public ReadOnlyReactiveProperty<double> RegisterDerivedGlobalProperty(TProperty name)
        {
            return RegisterProperty(PropertyId.New(name, EnumKey.ToEnum<TEntity>(_globalEntity), 0), false);
        }

        private ReadOnlyReactiveProperty<double> RegisterProperty(PropertyId propertyId, bool isBaseProperty, double initialBaseValue = 0)
        {
            var property = _store.Register(propertyId, isBaseProperty ? initialBaseValue : 0);

            var node = _graph.GetOrAddNode(GetFilterFromId(propertyId), isBaseProperty, out var wasAlreadyConnected);
            if (wasAlreadyConnected)
            {
                _propagator.MarkTreeNodeDirty(node);
            }

            return property.ReadOnlyValueReactive;
        }

        public void RegisterCalculator<T>() where T : IPropertyCalculator, new()
        {
            RegisterCalculator(new T());
        }

        public void RegisterCalculator(IPropertyCalculator calculator)
        {
            var node = _graph.Connect(calculator);
            _propagator.MarkTreeNodeDirty(node);
        }

        public void SetBasePropertyValue(TProperty name, TEntity entity, double value, int instanceIndex = 0)
        {
            var propertyId = PropertyId.New(name, entity, instanceIndex);
            var node = _graph.GetNode(GetFilterFromId(propertyId));

            if (node.Calculator is not BasePropertyCalculator)
            {
                throw new Exception("Directly modifying derived properties is not possible.");
            }

            _store.GetProperty(propertyId).ValueReactive.Value = value;
            _propagator.MarkPropertyDirty(propertyId, node, _store);
            _propagator.MarkTreeNodeDirty(node);
        }

        public void Tick()
        {
            IReadOnlyList<PropertyGraphNode> sortedNodes = _sorter.Sort(_propagator.DirtyNodes);
            _evaluator.Evaluate(sortedNodes, _store);
            _propagator.ClearDirtyNodes();
        }

        private static PropertyFilter GetFilterFromId(PropertyId id)
        {
            return PropertyFilter.New(id.Name, id.Entity);
        }

        public ReadOnlyReactiveProperty<double> this[TProperty name, TEntity entity, int instanceIndex]
            => _store.GetProperty(PropertyId.New(name, entity, instanceIndex)).ReadOnlyValueReactive;

        /// <summary>
        /// Renders the entity/property keys as the names of this system's enums.
        /// </summary>
        public string GetReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== PropertySystem snapshot ({_store.Count} properties) ===");
            foreach (var kv in _store.All)
            {
                var entity = Enum.GetName(typeof(TEntity), kv.Key.Entity) ?? kv.Key.Entity.ToString();
                var property = Enum.GetName(typeof(TProperty), kv.Key.Name) ?? kv.Key.Name.ToString();
                sb.AppendLine($"{entity}.{property}[{kv.Key.InstanceIndex}] = {kv.Value.ReadOnlyValueReactive.CurrentValue}");
            }

            return sb.ToString();
        }
    }
}