using System.Collections.Generic;
using GalacticExpansion.Core;
using UnityEngine;

namespace GalacticExpansion.Data
{
    /// <summary>
    /// Defines a resource used throughout the economy loop.
    /// </summary>
    [CreateAssetMenu(menuName = "Galactic Expansion/Resource", fileName = "Resource")]
    public sealed class ResourceDef : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private Sprite icon = null!;

        [Header("Presentation")]
        [SerializeField, Tooltip("Preferred number formatting for UI rendering.")]
        private ResourceDisplayFormat displayFormat = ResourceDisplayFormat.Scientific;

        [Header("Economy")]
        [SerializeField, Tooltip("Amount granted when a new run starts.")]
        private double startingAmount = 0d;

        [Header("Soft Caps")]
        [SerializeField, Tooltip("Optional soft cap thresholds applied after exceeding the specified amount.")]
        private List<SoftCapThreshold> softCapThresholds = new();

        /// <summary>
        /// Gets the unique identifier for the resource.
        /// </summary>
        public string Id => id;

        /// <summary>
        /// Gets the localized display name.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Gets the icon associated with the resource.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Gets the preferred UI display format.
        /// </summary>
        public ResourceDisplayFormat DisplayFormat => displayFormat;

        /// <summary>
        /// Gets the amount granted when starting a new run.
        /// </summary>
        public BigDouble StartingAmount => BigDouble.FromDouble(startingAmount);

        /// <summary>
        /// Gets the configured soft cap thresholds, if any.
        /// </summary>
        public IReadOnlyList<SoftCapThreshold> SoftCapThresholds => softCapThresholds;

        [System.Serializable]
        public sealed class SoftCapThreshold
        {
            [SerializeField, Tooltip("Resource amount at which the soft cap begins to apply.")]
            private double amount = 0d;

            [SerializeField, Tooltip("Exponent applied to production gains beyond this threshold (values between 0 and 1 reduce gains).")]
            private float exponent = 1f;

            /// <summary>
            /// Gets the amount at which the soft cap triggers.
            /// </summary>
            public BigDouble Amount => BigDouble.FromDouble(amount);

            /// <summary>
            /// Gets the exponent used when applying the cap.
            /// </summary>
            public float Exponent => exponent;
        }
    }

    /// <summary>
    /// Defines supported UI formats for displaying resource values.
    /// </summary>
    public enum ResourceDisplayFormat
    {
        /// <summary>
        /// Display values using classic numeric formatting.
        /// </summary>
        Standard,

        /// <summary>
        /// Display values using scientific notation (mantissa e exponent).
        /// </summary>
        Scientific,

        /// <summary>
        /// Display values using engineering notation aligned to multiples of three.
        /// </summary>
        Engineering
    }
}
