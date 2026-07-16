using System;
using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Kahn's algorithm restricted to the dirty subgraph: orders the dirty nodes so each node comes
    /// after every node it depends on, and throws if that subgraph contains a cycle. Reuses its
    /// scratch buffers so repeated sorts allocate nothing on the hot path.
    /// </summary>
    internal sealed class DirtySubgraphSorter : IDirtySubgraphSorter
    {
        private readonly Queue<PropertyGraphNode> _readyQueueCache = new();
        private readonly Dictionary<PropertyGraphNode, int> _inDegreeCache = new();
        private readonly List<PropertyGraphNode> _sorted = new();

        /// <summary>
        /// Returns the dirty nodes in evaluation order (dependencies first). The result is a
        /// read-only view of a buffer owned and reused by the sorter; the caller must consume it
        /// before the next <see cref="Sort"/>.
        /// </summary>
        public IReadOnlyList<PropertyGraphNode> Sort(IReadOnlyCollection<PropertyGraphNode> dirtyNodes)
        {
            _sorted.Clear();
            _inDegreeCache.Clear();

            foreach (var node in dirtyNodes)
            {
                _inDegreeCache[node] = 0;
            }

            foreach (var node in dirtyNodes)
            {
                if (node.Outputs == null)
                {
                    continue;
                }

                foreach (var output in node.Outputs)
                {
                    if (_inDegreeCache.ContainsKey(output))
                    {
                        _inDegreeCache[output] += 1;
                    }
                }
            }

            _readyQueueCache.Clear();
            foreach (var entry in _inDegreeCache)
            {
                if (entry.Value == 0)
                {
                    _readyQueueCache.Enqueue(entry.Key);
                }
            }

            while (_readyQueueCache.Count > 0)
            {
                var node = _readyQueueCache.Dequeue();
                _sorted.Add(node);

                if (node.Outputs == null)
                {
                    continue;
                }

                foreach (var output in node.Outputs)
                {
                    if (!_inDegreeCache.TryGetValue(output, out var remaining))
                    {
                        continue;
                    }

                    remaining--;
                    _inDegreeCache[output] = remaining;
                    if (remaining == 0)
                    {
                        _readyQueueCache.Enqueue(output);
                    }
                }
            }

            if (_sorted.Count != dirtyNodes.Count)
            {
                throw new Exception("Cycle detected in property dependency graph.");
            }

            return _sorted;
        }
    }
}
