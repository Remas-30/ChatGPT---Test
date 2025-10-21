using System.Collections.Generic;
using UnityEngine;

namespace GalacticExpansion.Data
{
    /// <summary>
    /// Defines a generator responsible for producing resources over time.
    /// </summary>
    [CreateAssetMenu(menuName = "Galactic Expansion/Generator", fileName = "Generator")]
    public sealed class GeneratorDef : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private Sprite icon = null!;
        [SerializeField] private List<string> tags = new();

        [Header("Economy")]
        [SerializeField, Tooltip("Base cost for the first level of this generator.")]
        private double baseCost = 10d;

        [SerializeField, Tooltip("Geometric multiplier applied per level when purchasing additional generators.")]
        private float costMultiplier = 1.15f;

        [SerializeField, Tooltip("Base production per second produced by a single level of this generator.")]
        private double baseProductionPerSecond = 1d;

        [SerializeField, Tooltip("Identifier of the resource produced by this generator.")]
        private string producesResourceId = string.Empty;

        [SerializeField, Tooltip("Identifier of the resource consumed to purchase this generator.")]
        private string requiresResourceId = string.Empty;

        [Header("Unlocking")]
        [SerializeField] private GeneratorUnlockCondition unlockCondition = new();

        /// <summary>
        /// Gets the unique identifier for the generator.
        /// </summary>
        public string Id => id;

        /// <summary>
        /// Gets the display name for UI presentation.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Gets the icon used in UI elements.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Gets the tags associated with this generator for multiplier lookups.
        /// </summary>
        public IReadOnlyList<string> Tags => tags;

        /// <summary>
        /// Gets the base cost for the generator.
        /// </summary>
        public double BaseCost => baseCost;

        /// <summary>
        /// Gets the geometric multiplier applied to the cost per level.
        /// </summary>
        public float CostMultiplier => costMultiplier;

        /// <summary>
        /// Gets the base production per second for a single level.
        /// </summary>
        public double BaseProductionPerSecond => baseProductionPerSecond;

        /// <summary>
        /// Gets the identifier of the resource produced.
        /// </summary>
        public string ProducesResourceId => producesResourceId;

        /// <summary>
        /// Gets the identifier of the resource required for purchases.
        /// </summary>
        public string RequiresResourceId => requiresResourceId;

        /// <summary>
        /// Gets the unlock condition definition.
        /// </summary>
        public GeneratorUnlockCondition UnlockCondition => unlockCondition;
    }

    /// <summary>
    /// Represents the requirements for unlocking a generator in-game.
    /// </summary>
    [System.Serializable]
    public sealed class GeneratorUnlockCondition
    {
        [SerializeField, Tooltip("If enabled the generator is available immediately.")]
        private bool unlockedByDefault = false;

        [SerializeField, Tooltip("Optional resource identifier that must reach the specified amount before unlocking.")]
        private string requiredResourceId = string.Empty;

        [SerializeField, Tooltip("Resource amount required to unlock the generator.")]
        private double requiredResourceAmount = 0d;

        [SerializeField, Tooltip("Optional map node identifier gate.")]
        private string requiredMapNodeId = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the generator starts unlocked.
        /// </summary>
        public bool UnlockedByDefault => unlockedByDefault;

        /// <summary>
        /// Gets the resource identifier tied to the unlock, if any.
        /// </summary>
        public string RequiredResourceId => requiredResourceId;

        /// <summary>
        /// Gets the amount of the required resource needed to unlock.
        /// </summary>
        public double RequiredResourceAmount => requiredResourceAmount;

        /// <summary>
        /// Gets the map node identifier gate, if any.
        /// </summary>
        public string RequiredMapNodeId => requiredMapNodeId;

        /// <summary>
        /// Returns true if a resource threshold gate is configured.
        /// </summary>
        public bool HasResourceGate => !string.IsNullOrEmpty(requiredResourceId) && requiredResourceAmount > 0d;

        /// <summary>
        /// Returns true if a map node gate is configured.
        /// </summary>
        public bool HasMapGate => !string.IsNullOrEmpty(requiredMapNodeId);
    }
}
