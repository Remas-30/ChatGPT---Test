using System;

namespace GalacticExpansion.Core
{
    /// <summary>
    /// Purchase quantity options shared across generators and upgrades.
    /// </summary>
    [Serializable]
    public enum PurchaseMode
    {
        /// <summary>
        /// Purchase a single level.
        /// </summary>
        X1,

        /// <summary>
        /// Purchase ten levels.
        /// </summary>
        X10,

        /// <summary>
        /// Purchase one hundred levels.
        /// </summary>
        X100,

        /// <summary>
        /// Purchase as many levels as the player can afford.
        /// </summary>
        Max
    }
}
