using System.Collections.Generic;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.DirtySubgraphSorterTests
{
    // Solitary, state-based tests for the pure ordering algorithm — no system, no calculators,
    // no mocks. A failure here points at the sort and nothing else.
    public class DirtySubgraphSorterTests
    {
        private DirtySubgraphSorter _sorter;

        [SetUp]
        public void SetUp()
        {
            _sorter = new DirtySubgraphSorter();
        }

        private static PropertyGraphNode Node() => new();

        private static void DependsOn(PropertyGraphNode dependent, PropertyGraphNode dependency)
        {
            // edge dependency -> dependent (dependency is an input, dependent is an output)
            (dependency.Outputs ??= new List<PropertyGraphNode>()).Add(dependent);
        }

        [Test]
        public void Sort_LinearChain_OrdersDependenciesFirst()
        {
            var a = Node();
            var b = Node();
            var c = Node();
            DependsOn(b, a); // b after a
            DependsOn(c, b); // c after b

            var sorted = _sorter.Sort(new HashSet<PropertyGraphNode> { a, b, c });

            Assert.That(sorted[0], Is.SameAs(a));
            Assert.That(sorted[1], Is.SameAs(b));
            Assert.That(sorted[2], Is.SameAs(c));
        }

        [Test]
        public void Sort_IncludesEveryDirtyNode()
        {
            var a = Node();
            var b = Node();
            DependsOn(b, a);

            var sorted = _sorter.Sort(new HashSet<PropertyGraphNode> { a, b });

            Assert.That(sorted.Count, Is.EqualTo(2));
        }

        [Test]
        public void Sort_DiamondDependency_PutsRootFirstAndJoinLast()
        {
            // a -> b, a -> c, b -> d, c -> d
            var a = Node();
            var b = Node();
            var c = Node();
            var d = Node();
            DependsOn(b, a);
            DependsOn(c, a);
            DependsOn(d, b);
            DependsOn(d, c);

            var sorted = _sorter.Sort(new HashSet<PropertyGraphNode> { a, b, c, d });

            Assert.That(sorted[0], Is.SameAs(a));
            Assert.That(sorted[3], Is.SameAs(d));
        }

        [Test]
        public void Sort_IgnoresEdgesToNodesOutsideTheDirtySet()
        {
            // a -> b, but only a is dirty; b is an output that isn't part of this sort.
            var a = Node();
            var b = Node();
            DependsOn(b, a);

            var sorted = _sorter.Sort(new HashSet<PropertyGraphNode> { a });

            Assert.That(sorted.Count, Is.EqualTo(1));
            Assert.That(sorted[0], Is.SameAs(a));
        }

        [Test]
        public void Sort_WithCycle_Throws()
        {
            var a = Node();
            var b = Node();
            DependsOn(b, a);
            DependsOn(a, b); // cycle

            Assert.Catch(() => _sorter.Sort(new HashSet<PropertyGraphNode> { a, b }));
        }

        [Test]
        public void Sort_EmptyDirtySet_ProducesEmptyOrder()
        {
            var sorted = _sorter.Sort(new HashSet<PropertyGraphNode>());

            Assert.That(sorted.Count, Is.EqualTo(0));
        }

        [Test]
        public void Sort_IsReusable_AcrossCalls()
        {
            var a = Node();
            var b = Node();
            DependsOn(b, a);
            _sorter.Sort(new HashSet<PropertyGraphNode> { a, b });

            var c = Node();
            var d = Node();
            DependsOn(d, c);
            var sorted = _sorter.Sort(new HashSet<PropertyGraphNode> { c, d });

            Assert.That(sorted.Count, Is.EqualTo(2));
            Assert.That(sorted[0], Is.SameAs(c));
            Assert.That(sorted[1], Is.SameAs(d));
        }
    }
}
