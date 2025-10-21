using System;
using System.Collections.Generic;
using GalacticExpansion.Core;

namespace GalacticExpansion.Services
{
    /// <summary>
    /// Tracks meta currencies such as Warp Cores that persist across prestige resets.
    /// </summary>
    public sealed class MetaCurrencyService : IGameService, ISaveable
    {
        private readonly Dictionary<string, BigDouble> _balances = new(StringComparer.OrdinalIgnoreCase);
        private bool _initialized;

        /// <summary>
        /// Raised when initialization completes.
        /// </summary>
        public event Action? ServiceReady;

        /// <inheritdoc />
        public string SaveKey => "meta";

        /// <inheritdoc />
        public void Initialize()
        {
            _initialized = true;
            ServiceReady?.Invoke();
        }

        /// <inheritdoc />
        public void Tick(double deltaTime)
        {
            // Meta currencies are not time-based.
        }

        /// <summary>
        /// Gets the current balance for the supplied currency identifier.
        /// </summary>
        public BigDouble GetCurrency(string currencyId)
        {
            return _balances.TryGetValue(currencyId, out BigDouble value) ? value : BigDouble.Zero;
        }

        /// <summary>
        /// Adds the specified amount to the currency balance.
        /// </summary>
        public void Add(string currencyId, BigDouble amount)
        {
            if (amount.IsZero || string.IsNullOrEmpty(currencyId))
            {
                return;
            }

            BigDouble current = GetCurrency(currencyId);
            _balances[currencyId] = current + amount;
        }

        /// <summary>
        /// Attempts to spend the supplied amount from the currency balance.
        /// </summary>
        public bool TrySpend(string currencyId, BigDouble amount)
        {
            if (amount <= BigDouble.Zero || string.IsNullOrEmpty(currencyId))
            {
                return false;
            }

            BigDouble current = GetCurrency(currencyId);
            if (current < amount)
            {
                return false;
            }

            _balances[currencyId] = current - amount;
            return true;
        }

        /// <inheritdoc />
        public object CaptureState()
        {
            var save = new MetaSaveData
            {
                Entries = new List<MetaEntry>(_balances.Count)
            };

            foreach (var pair in _balances)
            {
                save.Entries.Add(new MetaEntry
                {
                    Id = pair.Key,
                    Mantissa = pair.Value.Mantissa,
                    Exponent = pair.Value.Exponent
                });
            }

            return save;
        }

        /// <inheritdoc />
        public void RestoreState(object state)
        {
            if (state is not MetaSaveData save)
            {
                return;
            }

            _balances.Clear();
            foreach (MetaEntry entry in save.Entries)
            {
                if (string.IsNullOrEmpty(entry.Id))
                {
                    continue;
                }

                _balances[entry.Id] = BigDouble.FromUnnormalized(entry.Mantissa, entry.Exponent);
            }
        }

        [Serializable]
        private sealed class MetaSaveData
        {
            public List<MetaEntry> Entries = new();
        }

        [Serializable]
        private sealed class MetaEntry
        {
            public string Id = string.Empty;
            public double Mantissa;
            public int Exponent;
        }
    }
}
