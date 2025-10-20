using System;
using System.Collections.Generic;
using GalacticExpansion.Core;
using GalacticExpansion.Data;

namespace GalacticExpansion.Services
{
    /// <summary>
    /// Manages upgrade purchases, unlock checks, and modifier aggregation for production math.
    /// </summary>
    public sealed class UpgradeService : IGameService, ISaveable
    {
        private readonly Dictionary<string, UpgradeRuntime> _upgrades = new();
        private readonly List<UpgradeRuntime> _orderedUpgrades = new();
        private EconomyService? _economyService;
        private MapService? _mapService;
        private MetaCurrencyService? _metaCurrencyService;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpgradeService"/> class.
        /// </summary>
        public UpgradeService(IEnumerable<UpgradeDef> upgradeDefinitions)
        {
            foreach (UpgradeDef def in upgradeDefinitions)
            {
                if (string.IsNullOrEmpty(def.Id))
                {
                    continue;
                }

                var runtime = new UpgradeRuntime(def);
                _upgrades[def.Id] = runtime;
                _orderedUpgrades.Add(runtime);
            }
        }

        /// <summary>
        /// Raised when the service finished initialization.
        /// </summary>
        public event Action? ServiceReady;

        /// <summary>
        /// Raised when an upgrade level changes.
        /// </summary>
        public event Action<UpgradeRuntime>? UpgradeLeveled;

        /// <inheritdoc />
        public string SaveKey => "upgrades";

        /// <summary>
        /// Binds external services used for unlock checks and spending.
        /// </summary>
        public void Bind(EconomyService economyService, MapService mapService, MetaCurrencyService metaCurrencyService)
        {
            _economyService = economyService;
            _mapService = mapService;
            _metaCurrencyService = metaCurrencyService;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            foreach (UpgradeRuntime runtime in _orderedUpgrades)
            {
                runtime.Unlocked = runtime.Def.UnlockCondition.UnlockedByDefault;
            }

            UpdateUnlockStates();
            _initialized = true;
            ServiceReady?.Invoke();
        }

        /// <inheritdoc />
        public void Tick(double deltaTime)
        {
            if (!_initialized)
            {
                return;
            }

            UpdateUnlockStates();
        }

        /// <summary>
        /// Enumerates all upgrade runtimes.
        /// </summary>
        public IEnumerable<UpgradeRuntime> EnumerateUpgrades() => _orderedUpgrades;

        /// <summary>
        /// Enumerates upgrade definitions for UI binding.
        /// </summary>
        public IEnumerable<UpgradeDef> EnumerateUpgradesDefinitions()
        {
            foreach (UpgradeRuntime runtime in _orderedUpgrades)
            {
                yield return runtime.Def;
            }
        }

        /// <summary>
        /// Attempts to retrieve runtime data for an upgrade.
        /// </summary>
        public bool TryGetRuntime(string upgradeId, out UpgradeRuntime runtime) => _upgrades.TryGetValue(upgradeId, out runtime!);

        /// <summary>
        /// Returns the aggregated modifier for the provided tag and type.
        /// </summary>
        public double GetModifierForTag(string tag, UpgradeEffectType type)
        {
            double additive = 0d;
            double multiplicative = 1d;
            double exponential = 1d;

            foreach (UpgradeRuntime runtime in _orderedUpgrades)
            {
                if (runtime.Level <= 0 || !runtime.TargetsTag(tag))
                {
                    continue;
                }

                double value = runtime.Def.EffectValue;
                switch (runtime.Def.EffectType)
                {
                    case UpgradeEffectType.Additive:
                        additive += runtime.Level * value;
                        break;
                    case UpgradeEffectType.Multiplicative:
                        multiplicative *= Math.Pow(1d + value, runtime.Level);
                        break;
                    case UpgradeEffectType.Exponential:
                        exponential *= Math.Pow(Math.Max(0d, value), runtime.Level);
                        break;
                }
            }

            return type switch
            {
                UpgradeEffectType.Additive => additive,
                UpgradeEffectType.Multiplicative => multiplicative,
                UpgradeEffectType.Exponential => exponential,
                _ => 1d
            };
        }

        /// <summary>
        /// Gets the maximum affordable quantity for the provided upgrade.
        /// </summary>
        public int GetMaxAffordableQty(string upgradeId)
        {
            if (!_upgrades.TryGetValue(upgradeId, out UpgradeRuntime? runtime))
            {
                return 0;
            }

            int quantity = CalculateMaxAffordable(runtime);
            if (runtime.Def.MaxLevel >= 0)
            {
                int remaining = runtime.Def.MaxLevel - runtime.Level;
                quantity = Math.Max(0, Math.Min(quantity, remaining));
            }

            return quantity;
        }

        /// <summary>
        /// Computes the total cost for purchasing the next quantity levels of the upgrade.
        /// </summary>
        public BigDouble GetNextCost(string upgradeId, int quantity)
        {
            if (!_upgrades.TryGetValue(upgradeId, out UpgradeRuntime? runtime) || quantity <= 0)
            {
                return BigDouble.Zero;
            }

            return CalculateTotalCost(runtime, quantity);
        }

        /// <summary>
        /// Attempts to buy an upgrade level using the provided mode.
        /// </summary>
        public bool TryBuy(string upgradeId, PurchaseMode mode)
        {
            if (!_initialized || !_upgrades.TryGetValue(upgradeId, out UpgradeRuntime? runtime))
            {
                return false;
            }

            if (!runtime.Unlocked)
            {
                return false;
            }

            int quantity = ResolveQuantity(runtime, mode);
            if (quantity <= 0)
            {
                return false;
            }

            BigDouble totalCost = CalculateTotalCost(runtime, quantity);
            if (!TrySpend(runtime.Def.CostResourceId, totalCost))
            {
                return false;
            }

            runtime.Level += quantity;
            UpgradeLeveled?.Invoke(runtime);
            return true;
        }

        /// <summary>
        /// Determines whether the upgrade is unlocked.
        /// </summary>
        public bool IsUnlocked(string upgradeId)
        {
            return _upgrades.TryGetValue(upgradeId, out UpgradeRuntime? runtime) && runtime.Unlocked;
        }

        /// <summary>
        /// Resets non-permanent upgrades after a prestige reset.
        /// </summary>
        public void ResetForPrestige(Func<UpgradeRuntime, bool>? shouldReset = null)
        {
            foreach (UpgradeRuntime runtime in _orderedUpgrades)
            {
                if (shouldReset == null || shouldReset(runtime))
                {
                    runtime.Level = 0;
                }
            }

            UpdateUnlockStates();
        }

        /// <inheritdoc />
        public object CaptureState()
        {
            var save = new UpgradeSaveData
            {
                Levels = new List<UpgradeLevelEntry>(_orderedUpgrades.Count)
            };

            foreach (UpgradeRuntime runtime in _orderedUpgrades)
            {
                save.Levels.Add(new UpgradeLevelEntry
                {
                    Id = runtime.Def.Id,
                    Level = runtime.Level
                });
            }

            return save;
        }

        /// <inheritdoc />
        public void RestoreState(object state)
        {
            switch (state)
            {
                case UpgradeSaveData saveData:
                    foreach (UpgradeLevelEntry entry in saveData.Levels)
                    {
                        if (_upgrades.TryGetValue(entry.Id, out UpgradeRuntime? runtime))
                        {
                            int level = entry.Level;
                            if (runtime.Def.MaxLevel >= 0)
                            {
                                level = Math.Min(level, runtime.Def.MaxLevel);
                            }

                            runtime.Level = Math.Max(0, level);
                        }
                    }

                    break;
                case UpgradeSave legacy:
                    foreach (string id in legacy.Purchased)
                    {
                        if (_upgrades.TryGetValue(id, out UpgradeRuntime? runtime))
                        {
                            runtime.Level = runtime.Def.MaxLevel >= 0 ? Math.Min(1, runtime.Def.MaxLevel) : 1;
                        }
                    }

                    break;
            }
        }

        private void UpdateUnlockStates()
        {
            foreach (UpgradeRuntime runtime in _orderedUpgrades)
            {
                if (runtime.Unlocked)
                {
                    continue;
                }

                UpgradeUnlockCondition condition = runtime.Def.UnlockCondition;
                bool unlocked = condition.UnlockedByDefault;
                if (!unlocked && condition.HasMapGate && _mapService != null)
                {
                    unlocked |= _mapService.IsUnlocked(condition.RequiredMapNodeId);
                }

                if (!unlocked && condition.HasResourceGate && _economyService != null)
                {
                    BigDouble amount = _economyService.GetResourceAmount(condition.RequiredResourceId);
                    unlocked = amount >= BigDouble.FromDouble(condition.RequiredResourceAmount);
                }

                runtime.Unlocked = unlocked;
            }
        }

        private int ResolveQuantity(UpgradeRuntime runtime, PurchaseMode mode)
        {
            int desired = mode switch
            {
                PurchaseMode.X10 => 10,
                PurchaseMode.X100 => 100,
                PurchaseMode.Max => CalculateMaxAffordable(runtime),
                _ => 1
            };

            if (runtime.Def.MaxLevel >= 0)
            {
                int remaining = runtime.Def.MaxLevel - runtime.Level;
                desired = Math.Max(0, Math.Min(desired, remaining));
            }

            return desired;
        }

        private int CalculateMaxAffordable(UpgradeRuntime runtime)
        {
            BigDouble available = GetAvailable(runtime.Def.CostResourceId);
            if (available.IsZero)
            {
                return 0;
            }

            double multiplier = runtime.Def.CostMultiplier;
            BigDouble startCost = BigDouble.FromDouble(runtime.Def.BaseCost) * BigDouble.Pow(multiplier, runtime.Level);
            if (startCost.IsZero)
            {
                return 0;
            }

            if (Math.Abs(multiplier - 1d) < 1e-6d)
            {
                double ratio = (available / startCost).ToDouble();
                return Math.Max(0, (int)Math.Floor(ratio));
            }

            BigDouble multiplierMinusOne = BigDouble.FromDouble(multiplier - 1d);
            BigDouble ratio = (available * multiplierMinusOne / startCost) + BigDouble.One;
            if (ratio <= BigDouble.One)
            {
                return 0;
            }

            double n = Math.Floor(ratio.Log10() / Math.Log10(multiplier));
            int quantity = Math.Max(0, (int)n);
            while (quantity > 0 && CalculateTotalCost(runtime, quantity) > available)
            {
                quantity--;
            }

            if (runtime.Def.MaxLevel >= 0)
            {
                int remaining = runtime.Def.MaxLevel - runtime.Level;
                quantity = Math.Max(0, Math.Min(quantity, remaining));
            }

            return quantity;
        }

        private BigDouble CalculateTotalCost(UpgradeRuntime runtime, int quantity)
        {
            if (quantity <= 0)
            {
                return BigDouble.Zero;
            }

            double multiplier = runtime.Def.CostMultiplier;
            BigDouble baseCost = BigDouble.FromDouble(runtime.Def.BaseCost) * BigDouble.Pow(multiplier, runtime.Level);
            if (Math.Abs(multiplier - 1d) < 1e-6d)
            {
                return baseCost * BigDouble.FromDouble(quantity);
            }

            BigDouble numerator = BigDouble.Pow(multiplier, quantity) - BigDouble.One;
            BigDouble denominator = BigDouble.FromDouble(multiplier) - BigDouble.One;
            return baseCost * (numerator / denominator);
        }

        private bool TrySpend(string costResourceId, BigDouble amount)
        {
            if (_economyService != null && _economyService.TrySpend(costResourceId, amount))
            {
                return true;
            }

            return _metaCurrencyService != null && _metaCurrencyService.TrySpend(costResourceId, amount);
        }

        private BigDouble GetAvailable(string resourceId)
        {
            BigDouble amount = BigDouble.Zero;
            if (_economyService != null)
            {
                amount = _economyService.GetResourceAmount(resourceId);
            }

            if (_metaCurrencyService != null)
            {
                amount = BigDouble.Max(amount, _metaCurrencyService.GetCurrency(resourceId));
            }

            return amount;
        }

        [Serializable]
        private sealed class UpgradeSave
        {
            public List<string> Purchased = new();
        }

        [Serializable]
        private sealed class UpgradeSaveData
        {
            public List<UpgradeLevelEntry> Levels = new();
        }

        [Serializable]
        private sealed class UpgradeLevelEntry
        {
            public string Id = string.Empty;
            public int Level;
        }

        /// <summary>
        /// Runtime data associated with an upgrade definition.
        /// </summary>
        public sealed class UpgradeRuntime
        {
            internal UpgradeRuntime(UpgradeDef def)
            {
                Def = def;
            }

            /// <summary>
            /// Gets the backing definition.
            /// </summary>
            public UpgradeDef Def { get; }

            /// <summary>
            /// Gets or sets the current level.
            /// </summary>
            public int Level { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the upgrade is unlocked.
            /// </summary>
            public bool Unlocked { get; set; }

            /// <summary>
            /// Returns true if this upgrade targets the provided tag.
            /// </summary>
            public bool TargetsTag(string tag)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return false;
                }

                foreach (string upgradeTag in Def.Tags)
                {
                    if (string.Equals(upgradeTag, tag, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
