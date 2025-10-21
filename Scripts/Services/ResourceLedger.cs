using System.Collections.Generic;
using GalacticExpansion.Core;

namespace GalacticExpansion.Services
{
    /// <summary>
    /// Tracks runtime resource balances with convenience helpers for idle economy logic.
    /// </summary>
    public sealed class ResourceLedger
    {
        private readonly Dictionary<string, BigDouble> _balances = new();

        /// <summary>
        /// Clears all stored balances.
        /// </summary>
        public void Clear() => _balances.Clear();

        /// <summary>
        /// Sets the balance for the supplied resource identifier.
        /// </summary>
        public void Set(string resourceId, BigDouble amount) => _balances[resourceId] = amount;

        /// <summary>
        /// Gets the balance for the supplied resource identifier.
        /// </summary>
        public BigDouble Get(string resourceId) => _balances.TryGetValue(resourceId, out BigDouble amount) ? amount : BigDouble.Zero;

        /// <summary>
        /// Adds the specified amount to the tracked balance.
        /// </summary>
        public void Add(string resourceId, BigDouble amount)
        {
            if (amount.IsZero)
            {
                return;
            }

            _balances[resourceId] = Get(resourceId) + amount;
        }

        /// <summary>
        /// Returns true when the ledger can cover the specified cost.
        /// </summary>
        public bool CanAfford(string resourceId, BigDouble cost) => Get(resourceId) >= cost;

        /// <summary>
        /// Attempts to remove the specified cost from the ledger.
        /// </summary>
        public bool Spend(string resourceId, BigDouble cost)
        {
            if (!CanAfford(resourceId, cost))
            {
                return false;
            }

            _balances[resourceId] = Get(resourceId) - cost;
            return true;
        }

        /// <summary>
        /// Enumerates all tracked balances.
        /// </summary>
        public IEnumerable<KeyValuePair<string, BigDouble>> EnumerateBalances() => _balances;
    }
}
