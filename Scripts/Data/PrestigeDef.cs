using UnityEngine;

namespace GalacticExpansion.Data
{
    /// <summary>
    /// Supported prestige tiers.
    /// </summary>
    public enum PrestigeTier
    {
        /// <summary>
        /// First prestige layer that awards Warp Cores.
        /// </summary>
        Warp = 1,

        /// <summary>
        /// Second prestige layer reserved for later phases.
        /// </summary>
        Ascension = 2,

        /// <summary>
        /// Late-game prestige layer reserved for future work.
        /// </summary>
        Singularity = 3
    }

    /// <summary>
    /// Defines prestige requirements and reward tuning.
    /// </summary>
    [CreateAssetMenu(menuName = "Galactic Expansion/Prestige", fileName = "Prestige")]
    public sealed class PrestigeDef : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private PrestigeTier tier = PrestigeTier.Warp;
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;

        [Header("Requirements")]
        [SerializeField, Tooltip("Identifier of the lifetime metric used to determine eligibility (e.g. Credits).")]
        private string requirementMetricId = string.Empty;
        [SerializeField, Tooltip("Minimum value of the requirement metric before prestige becomes available.")]
        private double requiredMetricValue = 1e6d;

        [Header("Rewards")]
        [SerializeField, Tooltip("Identifier of the meta currency awarded on prestige.")]
        private string rewardCurrencyId = string.Empty;
        [SerializeField, Tooltip("Coefficient A in the reward formula: floor(A * pow(max(1, metric/B), exponent))")]
        private double rewardCoefficient = 1d;
        [SerializeField, Tooltip("Divisor B in the reward formula.")]
        private double rewardDivisor = 1e6d;
        [SerializeField, Tooltip("Exponent applied to the normalized metric value.")]
        private float rewardExponent = 0.5f;

        /// <summary>
        /// Gets the prestige tier.
        /// </summary>
        public PrestigeTier Tier => tier;

        /// <summary>
        /// Gets the unique identifier for the prestige definition.
        /// </summary>
        public string Id => id;

        /// <summary>
        /// Gets the display name used in UI elements.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Gets the descriptive text shown to the player.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Gets the identifier of the metric that controls prestige eligibility.
        /// </summary>
        public string RequirementMetricId => requirementMetricId;

        /// <summary>
        /// Gets the minimum metric value required before prestige is unlocked.
        /// </summary>
        public double RequiredMetricValue => requiredMetricValue;

        /// <summary>
        /// Gets the identifier of the meta currency awarded when prestiging.
        /// </summary>
        public string RewardCurrencyId => rewardCurrencyId;

        /// <summary>
        /// Gets the coefficient applied to the prestige reward formula.
        /// </summary>
        public double RewardCoefficient => rewardCoefficient;

        /// <summary>
        /// Gets the divisor applied when normalizing the metric value.
        /// </summary>
        public double RewardDivisor => rewardDivisor;

        /// <summary>
        /// Gets the exponent applied to the normalized metric value.
        /// </summary>
        public float RewardExponent => rewardExponent;
    }
}
