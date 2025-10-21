using System.Collections.Generic;
using UnityEngine;

namespace GalacticExpansion.Data
{
    /// <summary>
    /// Enumerates supported upgrade effect behaviours.
    /// </summary>
    public enum UpgradeEffectType
    {
        /// <summary>
        /// Adds a percentage bonus that is summed with other additive upgrades.
        /// </summary>
        Additive,

        /// <summary>
        /// Multiplies production by a factor per level.
        /// </summary>
        Multiplicative,

        /// <summary>
        /// Raises the base production to a power per level.
        /// </summary>
        Exponential
    }

    /// <summary>
    /// Describes a purchasable upgrade that modifies generator or resource output.
    /// </summary>
    [CreateAssetMenu(menuName = "Galactic Expansion/Upgrade", fileName = "Upgrade")]
    public sealed class UpgradeDef : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private Sprite icon = null!;

        [Header("Costs")]
        [SerializeField, Tooltip("Identifier of the resource or meta currency used to purchase the upgrade.")]
        private string costResourceId = string.Empty;
        [SerializeField, Tooltip("Base cost for the first level of this upgrade.")]
        private double baseCost = 10d;
        [SerializeField, Tooltip("Multiplier applied to the cost for each subsequent level.")]
        private float costMultiplier = 1.15f;

        [Header("Effects")]
        [SerializeField, Tooltip("Tags that determine which generators/resources receive this upgrade.")]
        private List<string> tags = new();
        [SerializeField, Tooltip("Type of effect applied per level.")]
        private UpgradeEffectType effectType = UpgradeEffectType.Multiplicative;
        [SerializeField, Tooltip("Magnitude of the effect per level. For additive use percentages (0.05 = +5%).")]
        private double effectValue = 0.1d;
        [SerializeField, Tooltip("Maximum level allowed. Set to -1 for infinite scaling."), Min(-1)]
        private int maxLevel = -1;

        [Header("Unlocking")]
        [SerializeField, Tooltip("Optional unlock requirements that gate this upgrade.")]
        private UpgradeUnlockCondition unlockCondition = new();

        /// <summary>
        /// Gets the unique identifier for the upgrade.
        /// </summary>
        public string Id => id;

        /// <summary>
        /// Gets the localized display name.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Gets the descriptive text.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Gets the icon used within UI elements.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Gets the identifier of the resource or meta currency used for purchases.
        /// </summary>
        public string CostResourceId => costResourceId;

        /// <summary>
        /// Gets the base cost of the first level.
        /// </summary>
        public double BaseCost => baseCost;

        /// <summary>
        /// Gets the geometric cost multiplier applied each level.
        /// </summary>
        public float CostMultiplier => costMultiplier;

        /// <summary>
        /// Gets the tags targeted by this upgrade.
        /// </summary>
        public IReadOnlyList<string> Tags => tags;

        /// <summary>
        /// Gets the effect type applied per level.
        /// </summary>
        public UpgradeEffectType EffectType => effectType;

        /// <summary>
        /// Gets the magnitude of the effect per level.
        /// </summary>
        public double EffectValue => effectValue;

        /// <summary>
        /// Gets the maximum purchase level (-1 for infinite).
        /// </summary>
        public int MaxLevel => maxLevel;

        /// <summary>
        /// Gets the unlock condition definition.
        /// </summary>
        public UpgradeUnlockCondition UnlockCondition => unlockCondition;
    }

    /// <summary>
    /// Represents optional requirements that gate an upgrade from being purchased.
    /// </summary>
    [System.Serializable]
    public sealed class UpgradeUnlockCondition
    {
        [SerializeField, Tooltip("If enabled the upgrade is available immediately.")]
        private bool unlockedByDefault = false;
        [SerializeField, Tooltip("Optional resource identifier required to reach a threshold before unlocking.")]
        private string requiredResourceId = string.Empty;
        [SerializeField, Tooltip("Amount of the resource required to unlock the upgrade.")]
        private double requiredResourceAmount = 0d;
        [SerializeField, Tooltip("Optional map node identifier that must be discovered before purchase.")]
        private string requiredMapNodeId = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the upgrade starts unlocked.
        /// </summary>
        public bool UnlockedByDefault => unlockedByDefault;

        /// <summary>
        /// Gets the resource identifier gate, if any.
        /// </summary>
        public string RequiredResourceId => requiredResourceId;

        /// <summary>
        /// Gets the resource amount gate, if any.
        /// </summary>
        public double RequiredResourceAmount => requiredResourceAmount;

        /// <summary>
        /// Gets the required map node identifier, if any.
        /// </summary>
        public string RequiredMapNodeId => requiredMapNodeId;

        /// <summary>
        /// Returns true if the unlock has a resource requirement.
        /// </summary>
        public bool HasResourceGate => !string.IsNullOrEmpty(requiredResourceId) && requiredResourceAmount > 0d;

        /// <summary>
        /// Returns true if the unlock has a map node requirement.
        /// </summary>
        public bool HasMapGate => !string.IsNullOrEmpty(requiredMapNodeId);
    }
}
