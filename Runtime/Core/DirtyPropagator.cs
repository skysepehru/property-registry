using System.Collections.Generic;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Propagates dirtiness through the dependency graph: which calculator nodes must re-run
    /// (<see cref="MarkTreeNodeDirty"/>) and which individual property instances are affected by a
    /// change (<see cref="MarkPropertyDirty"/>), including the global fan-out rules.
    /// </summary>
    internal sealed class DirtyPropagator : IDirtyPropagator
    {
        private readonly int _globalEntity;

        private readonly Queue<PropertyGraphNode> _queueCache = new();

        // Property propagation carries a per-path "passed through global" flag, so visited state is
        // keyed on (node, passedGlobal): a node can legitimately be reached both ways (diamond).
        private readonly Queue<(PropertyGraphNode node, bool passedGlobal)> _propagationQueue = new();
        private readonly HashSet<(PropertyGraphNode node, bool passedGlobal)> _propagationVisited = new();

        private readonly HashSet<PropertyGraphNode> _dirtyNodes = new();

        public DirtyPropagator(int globalEntity)
        {
            _globalEntity = globalEntity;
        }

        public IReadOnlyCollection<PropertyGraphNode> DirtyNodes => _dirtyNodes;

        public void ClearDirtyNodes() => _dirtyNodes.Clear();

        public void MarkTreeNodeDirty(PropertyGraphNode node)
        {
            if (_dirtyNodes.Contains(node))
            {
                return;
            }

            _queueCache.Clear();
            _queueCache.Enqueue(node);

            while (_queueCache.Count > 0)
            {
                var dirtyNode = _queueCache.Dequeue();
                if (!_dirtyNodes.Add(dirtyNode) || dirtyNode.Outputs == null)
                {
                    continue;
                }

                foreach (var output in dirtyNode.Outputs)
                {
                    _queueCache.Enqueue(output);
                }
            }
        }

        /// <summary>
        /// Flags the property instances affected by a change to <paramref name="propertyId"/>. Along a
        /// path that stays within one entity, only the matching instance index is marked. Once a path
        /// passes through the global entity (or crosses between different entities), all downstream
        /// instances on that path are marked. "Passed through global" is tracked per path, so a sibling
        /// path that never touches the global entity keeps its per-instance marking.
        /// </summary>
        public void MarkPropertyDirty(PropertyId propertyId, PropertyGraphNode startNode, IPropertyStore store)
        {
            store.GetProperty(propertyId).IsDirtied = true;

            _propagationQueue.Clear();
            _propagationVisited.Clear();
            _propagationQueue.Enqueue((startNode, false));

            while (_propagationQueue.Count > 0)
            {
                var (currentNode, passedGlobal) = _propagationQueue.Dequeue();
                if (!_propagationVisited.Add((currentNode, passedGlobal)) || currentNode.Outputs == null)
                {
                    continue;
                }

                var currentEntity = currentNode.PropertyFilter.Entity;
                var involvesGlobal = passedGlobal || currentEntity == _globalEntity;

                foreach (var outputNode in currentNode.Outputs)
                {
                    var outputEntity = outputNode.PropertyFilter.Entity;
                    var outputName = outputNode.PropertyFilter.Name;

                    if (outputEntity == _globalEntity)
                    {
                        store.GetEntityPropertyInstances(outputEntity, outputName)[0].IsDirtied = true;
                    }
                    else if (involvesGlobal)
                    {
                        MarkAllInstances(store, outputEntity, outputName);
                    }
                    else if (currentEntity == outputEntity)
                    {
                        store.GetEntityPropertyInstances(outputEntity, outputName)[propertyId.InstanceIndex].IsDirtied = true;
                    }
                    else
                    {
                        MarkAllInstances(store, outputEntity, outputName);
                    }

                    _propagationQueue.Enqueue((outputNode, involvesGlobal));
                }
            }
        }

        private static void MarkAllInstances(IPropertyStore store, int entity, int name)
        {
            var instances = store.GetEntityPropertyInstances(entity, name);
            for (int i = 0; i < instances.Count; i++)
            {
                instances[i].IsDirtied = true;
            }
        }
    }
}
