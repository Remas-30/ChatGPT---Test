using System;
using System.Collections.Generic;
using GalacticExpansion.Core;
using GalacticExpansion.Data;

namespace GalacticExpansion.Services
{
    /// <summary>
    /// Handles prestige layers starting with the Warp tier.
    /// </summary>
    public sealed class PrestigeService : IGameService, ISaveable
    {
        private readonly Dictionary<string, PrestigeDef> _prestigeDefs = new();
        private readonly EconomyService _economyService;
        private readonly MapService _mapService;
        private readonly UpgradeService _upgradeService;
        private readonly MetaCurrencyService _metaService;
        private readonly GameBalance _balance;
        private bool _initialized;
        private bool _warpEligible;
        private DateTime _lastPrestigeUtc;
        private int _warpCount;

        /// <summary>
        /// Raised when initialization completes.
        /// </summary>
        public event Action? ServiceReady;

        /// <summary>
        /// Raised when the Warp eligibility state changes.
        /// </summary>
        public event Action<bool>? WarpEligibilityChanged;

        /// <summary>
        /// Raised when a prestige reset completes.
        /// </summary>
        public event Action<PrestigeDef, BigDouble>? PrestigePerformed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrestigeService"/> class.
        /// </summary>
        public PrestigeService(PrestigeDef[] prestigeDefs, EconomyService economyService, MapService mapService, UpgradeService upgradeService, MetaCurrencyService metaService, GameBalance balance)
        {
            _economyService = economyService;
            _mapService = mapService;
            _upgradeService = upgradeService;
            _metaService = metaService;
            _balance = balance;
            foreach (PrestigeDef def in prestigeDefs)
            {
                if (!string.IsNullOrEmpty(def.Id))
                {
                    _prestigeDefs[def.Id] = def;
                }
            }
        }

        /// <inheritdoc />
        public string SaveKey => "prestige";

        /// <inheritdoc />
        public void Initialize()
        {
            _initialized = true;
            ServiceReady?.Invoke();
            UpdateEligibility();
        }

        /// <inheritdoc />
        public void Tick(double deltaTime)
        {
            if (!_initialized)
            {
                return;
            }

            UpdateEligibility();
        }

        /// <summary>
        /// Gets the predicted Warp reward based on current lifetime production.
        /// </summary>
        public BigDouble GetProjectedWarpReward()
        {
            if (!TryGetPrestige(PrestigeTier.Warp, out PrestigeDef def))
            {
                return BigDouble.Zero;
            }

            BigDouble metric = GetRequirementMetric(def);
            return CalculateWarpReward(def, metric);
        }

        /// <summary>
        /// Determines whether Warp prestige is currently available.
        /// </summary>
        public bool IsWarpEligible => _warpEligible;

        /// <summary>
        /// Attempts to perform a Warp prestige reset.
        /// </summary>
        public bool TryPrestigeWarp()
        {
            if (!_initialized || !TryGetPrestige(PrestigeTier.Warp, out PrestigeDef def))
            {
                return false;
            }

            BigDouble metric = GetRequirementMetric(def);
            double requirement = Math.Max(def.RequiredMetricValue, _balance.WarpRequirement);
            if (metric < BigDouble.FromDouble(requirement))
            {
                return false;
            }

            BigDouble reward = CalculateWarpReward(def, metric);
            if (reward <= BigDouble.Zero)
            {
                return false;
            }

            PerformWarp(def, reward);
            return true;
        }

        /// <summary>
        /// Gets the current Warp requirement progress and required threshold.
        /// </summary>
        public BigDouble GetWarpRequirementProgress(out double requirement)
        {
            requirement = 0d;
            if (!TryGetPrestige(PrestigeTier.Warp, out PrestigeDef def))
            {
                return BigDouble.Zero;
            }

            requirement = Math.Max(def.RequiredMetricValue, _balance.WarpRequirement);
            return GetRequirementMetric(def);
        }

        /// <summary>
        /// Attempts to expose a prestige definition by tier.
        /// </summary>
        public bool TryGetPrestigeDefinition(PrestigeTier tier, out PrestigeDef def) => TryGetPrestige(tier, out def);

        /// <inheritdoc />
        public object CaptureState()
        {
            return new PrestigeSave
            {
                LastPrestigeUtc = _lastPrestigeUtc.ToUniversalTime().ToString("o"),
                WarpCount = _warpCount
            };
        }

        /// <inheritdoc />
        public void RestoreState(object state)
        {
            if (state is not PrestigeSave save)
            {
                return;
            }

            if (DateTime.TryParse(save.LastPrestigeUtc, out DateTime parsed))
            {
                _lastPrestigeUtc = parsed;
            }

            _warpCount = Math.Max(0, save.WarpCount);
        }

        private void PerformWarp(PrestigeDef def, BigDouble reward)
        {
            _economyService.ResetResourcesToBase();
            _economyService.ResetGenerators();
            _upgradeService.ResetForPrestige(runtime => !string.Equals(runtime.Def.CostResourceId, def.RewardCurrencyId, StringComparison.OrdinalIgnoreCase));
            _mapService.Initialize();
            _metaService.Add(def.RewardCurrencyId, reward);

            _warpCount++;
            _lastPrestigeUtc = DateTime.UtcNow;
            PrestigePerformed?.Invoke(def, reward);
            UpdateEligibility(forceNotify: true);
        }

        private void UpdateEligibility(bool forceNotify = false)
        {
            if (!TryGetPrestige(PrestigeTier.Warp, out PrestigeDef def))
            {
                return;
            }

            double requirement = Math.Max(def.RequiredMetricValue, _balance.WarpRequirement);
            BigDouble metric = GetRequirementMetric(def);
            bool eligible = metric >= BigDouble.FromDouble(requirement);
            if (forceNotify || eligible != _warpEligible)
            {
                _warpEligible = eligible;
                WarpEligibilityChanged?.Invoke(_warpEligible);
            }
        }

        private BigDouble CalculateWarpReward(PrestigeDef def, BigDouble metric)
        {
            double coefficient = def.RewardCoefficient <= 0d ? _balance.WarpRewardCoefficient : def.RewardCoefficient;
            double divisor = def.RewardDivisor <= 0d ? _balance.WarpRewardDivisor : def.RewardDivisor;
            double exponent = def.RewardExponent <= 0f ? _balance.WarpRewardExponent : def.RewardExponent;
            BigDouble normalized = metric / BigDouble.FromDouble(divisor);
            if (normalized < BigDouble.One)
            {
                normalized = BigDouble.One;
            }

            BigDouble scaled = normalized.Pow(exponent);
            BigDouble reward = scaled * BigDouble.FromDouble(coefficient);
            double floored = Math.Floor(Math.Max(0d, reward.ToDouble()));
            return BigDouble.FromDouble(floored);
        }

        private BigDouble GetRequirementMetric(PrestigeDef def)
        {
            if (string.IsNullOrEmpty(def.RequirementMetricId))
            {
                return BigDouble.Zero;
            }

            return _economyService.GetLifetimeProduced(def.RequirementMetricId);
        }

        private bool TryGetPrestige(PrestigeTier tier, out PrestigeDef def)
        {
            foreach (PrestigeDef candidate in _prestigeDefs.Values)
            {
                if (candidate.Tier == tier)
                {
                    def = candidate;
                    return true;
                }
            }

            def = null!;
            return false;
        }

        [Serializable]
        private sealed class PrestigeSave
        {
            public string LastPrestigeUtc = string.Empty;
            public int WarpCount;
        }
    }
}
