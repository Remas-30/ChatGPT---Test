using System;
using System.Collections.Generic;
using GalacticExpansion.Core;
using GalacticExpansion.Data;

namespace GalacticExpansion.Services
{
    /// <summary>
    /// Manages exploration map nodes and their associated bonuses.
    /// </summary>
    public sealed class MapService : IGameService, ISaveable
    {
        private readonly Dictionary<string, MapNodeDef> _nodes = new();
        private readonly HashSet<string> _unlockedNodes = new();
        private readonly List<string> _startingNodes = new();
        private float _globalMultiplier = 1f;
        private bool _initialized;

        public event Action? ServiceReady;
        public event Action<MapNodeDef>? MapNodeUnlocked;

        public MapService(MapNodeDef[] nodes)
        {
            foreach (MapNodeDef node in nodes)
            {
                _nodes[node.Id] = node;
            }
        }

        public string SaveKey => "map";

        public float OfflineEfficiency { get; private set; } = 0.5f;

        public void Initialize()
        {
            _unlockedNodes.Clear();
            _globalMultiplier = 1f;
            _startingNodes.Clear();
            foreach (MapNodeDef node in _nodes.Values)
            {
                if (node.RequiredNodes.Count == 0)
                {
                    _startingNodes.Add(node.Id);
                }
            }

            foreach (string nodeId in _startingNodes)
            {
                UnlockNode(nodeId);
            }

            _initialized = true;
            ServiceReady?.Invoke();
        }

        public void Tick(double deltaTime)
        {
        }

        public bool CanUnlock(MapNodeDef node, Func<string, BigDouble> resourceLookup)
        {
            for (int i = 0; i < node.RequiredResources.Count; i++)
            {
                string resourceId = node.RequiredResources[i];
                double requiredAmount = i < node.RequiredAmounts.Count ? node.RequiredAmounts[i] : 0d;
                if (resourceLookup(resourceId).ToDouble() < requiredAmount)
                {
                    return false;
                }
            }

            foreach (MapNodeDef required in node.RequiredNodes)
            {
                if (!_unlockedNodes.Contains(required.Id))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryUnlock(MapNodeDef node, EconomyService economy)
        {
            if (!CanUnlock(node, id => economy.GetResourceAmount(id)))
            {
                return false;
            }

            UnlockNode(node.Id);
            return true;
        }

        public void UnlockNode(string nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out MapNodeDef? node))
            {
                return;
            }

            if (_unlockedNodes.Add(nodeId))
            {
                _globalMultiplier *= node.ProductionMultiplier <= 0f ? 1f : node.ProductionMultiplier;
                MapNodeUnlocked?.Invoke(node);
            }
        }

        public float GetGlobalMultiplier() => _globalMultiplier;

        /// <summary>
        /// Determines whether the supplied node identifier has been unlocked.
        /// </summary>
        public bool IsUnlocked(string nodeId) => _unlockedNodes.Contains(nodeId);

        public object CaptureState()
        {
            return new MapSave
            {
                UnlockedNodeIds = new List<string>(_unlockedNodes),
                GlobalMultiplier = _globalMultiplier,
                OfflineEfficiency = OfflineEfficiency
            };
        }

        public void RestoreState(object state)
        {
            if (state is not MapSave save)
            {
                return;
            }

            _unlockedNodes.Clear();
            foreach (string id in save.UnlockedNodeIds)
            {
                _unlockedNodes.Add(id);
            }

            _globalMultiplier = save.GlobalMultiplier <= 0f ? 1f : save.GlobalMultiplier;
            OfflineEfficiency = save.OfflineEfficiency <= 0f ? OfflineEfficiency : save.OfflineEfficiency;
        }

        private sealed class MapSave
        {
            public List<string> UnlockedNodeIds = new();
            public float GlobalMultiplier = 1f;
            public float OfflineEfficiency = 0.5f;
        }
    }
}
