using UnityEngine;

namespace GalacticExpansion.Data
{
    /// <summary>
    /// Centralized balance tunables shared across multiple systems.
    /// </summary>
    [CreateAssetMenu(menuName = "Galactic Expansion/Game Balance", fileName = "GameBalance")]
    public sealed class GameBalance : ScriptableObject
    {
        [Header("Offline Progression")]
        [SerializeField, Tooltip("Efficiency multiplier applied to offline gains (0-1).")]
        private float offlineEfficiency = 0.9f;
        [SerializeField, Tooltip("Maximum hours of offline time rewarded on load.")]
        private float maxOfflineHours = 12f;

        [Header("Prestige - Warp")]
        [SerializeField, Tooltip("Coefficient A for the Warp reward formula.")]
        private double warpRewardCoefficient = 1d;
        [SerializeField, Tooltip("Divisor B for the Warp reward formula.")]
        private double warpRewardDivisor = 1e6d;
        [SerializeField, Tooltip("Exponent applied to the normalized Warp metric (0.5 = square root).")]
        private float warpRewardExponent = 0.5f;
        [SerializeField, Tooltip("Lifetime metric value required before Warp prestige becomes eligible.")]
        private double warpRequirement = 1e7d;
        [SerializeField, Tooltip("Audio key played when a Warp prestige completes.")]
        private string warpPrestigeSfxKey = "prestige_warp";

        /// <summary>
        /// Gets the offline efficiency multiplier applied to idle gains.
        /// </summary>
        public float OfflineEfficiency => offlineEfficiency;

        /// <summary>
        /// Gets the maximum offline hours rewarded on load.
        /// </summary>
        public float MaxOfflineHours => maxOfflineHours;

        /// <summary>
        /// Gets the Warp prestige reward coefficient A.
        /// </summary>
        public double WarpRewardCoefficient => warpRewardCoefficient;

        /// <summary>
        /// Gets the Warp prestige reward divisor B.
        /// </summary>
        public double WarpRewardDivisor => warpRewardDivisor;

        /// <summary>
        /// Gets the Warp prestige reward exponent.
        /// </summary>
        public float WarpRewardExponent => warpRewardExponent;

        /// <summary>
        /// Gets the minimum lifetime metric required to unlock Warp.
        /// </summary>
        public double WarpRequirement => warpRequirement;

        /// <summary>
        /// Gets the audio key triggered when a Warp prestige completes.
        /// </summary>
        public string WarpPrestigeSfxKey => warpPrestigeSfxKey;
    }
}
