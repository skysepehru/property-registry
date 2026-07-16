namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertySystemTests
{
    internal enum TestEntities
    {
        Global,
        Primary,
        Secondary,
    }

    internal enum TestProperties
    {
        Base,
        Derived,
        BaseGlobal,
        DerivedGlobal,
    }

    /// <summary>
    /// Concrete PropertySystem that exposes the internal injecting constructor, so the orchestrator can
    /// be driven over substituted subsystems in isolation.
    /// </summary>
    internal sealed class TestPropertySystem : PropertySystem<TestEntities, TestProperties>
    {
        public TestPropertySystem(
            IPropertyStore store,
            IPropertyDependencyGraph graph,
            IDirtyPropagator propagator,
            IDirtySubgraphSorter sorter,
            ICalculatorEvaluator evaluator)
            : base(TestEntities.Global, store, graph, propagator, sorter, evaluator)
        {
        }
    }

    /// <summary>
    /// Hand double for IPropertyCalculator (NSubstitute can't proxy its ref-struct context). PropertySystem
    /// only forwards calculators to the graph, so the fake needs no behaviour beyond a parameterless
    /// constructor (for the generic RegisterCalculator&lt;T&gt; overload) and a fixed output.
    /// </summary>
    internal sealed class FakeCalculator : IPropertyCalculator
    {
        public void Declare(CalculatorBuilder builder)
            => builder.SetOutput(TestProperties.Derived, TestEntities.Primary);

        public void Calculate(in CalculationContext context)
        {
        }
    }
}
