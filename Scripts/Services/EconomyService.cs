using System;
using System.Collections.Generic;
using GalacticExpansion.Core;
using GalacticExpansion.Data;

namespace GalacticExpansion.Services
{
    /// <summary>
    /// Manages resources, generators, and production logic for the idle economy.
    /// </summary>
    public sealed class EconomyService : IGameService, ISaveable
    {
        private const string AllGeneratorsTag = "AllGenerators";

        private readonly ResourceLedger _ledger = new();
        private readonly Dictionary<string, ResourceDef> _resourceDefs = new();
        private readonly Dictionary<string, GeneratorRuntime> _generators = new();
        private readonly Dictionary<string, BigDouble> _lifetimeProduced = new(StringComparer.OrdinalIgnoreCase);
        private readonly EventService _eventService;
        private readonly MapService _mapService;
        private readonly double _offlineEfficiency;
        private UpgradeService? _upgradeService;
        private bool _initialized;
        private bool _restoredFromSave;
        private double _eventMultiplier = 1d;

        /// <summary>
        /// Initializes a new instance of the <see cref="EconomyService"/> class.
        /// </summary>
        public EconomyService(EventService eventService, MapService mapService, double offlineEfficiency)
        {
            _eventService = eventService;
            _mapService = mapService;
            _offlineEfficiency = offlineEfficiency;
            _eventService.EventMultiplierChanged += OnEventMultiplierChanged;
        }

        /// <summary>
        /// Raised when the service has finished initialization.
        /// </summary>
        public event Action? ServiceReady;

        /// <inheritdoc />
        public string SaveKey => "economy";

        /// <summary>
        /// Binds the upgrade service so production math can query modifiers.
        /// </summary>
        public void BindUpgradeService(UpgradeService upgradeService)
        {
            _upgradeService = upgradeService;
        }

        /// <summary>
        /// Configures resource and generator definitions for the session.
        /// </summary>
        public void Configure(ResourceDef[] resources, GeneratorDef[] generators)
        {
            _resourceDefs.Clear();
            _generators.Clear();
            foreach (ResourceDef resource in resources)
            {
                if (string.IsNullOrEmpty(resource.Id))
                {
                    continue;
                }

                _resourceDefs[resource.Id] = resource;
                if (!_restoredFromSave)
                {
                    _ledger.Set(resource.Id, resource.StartingAmount);
                }

                _lifetimeProduced.TryAdd(resource.Id, BigDouble.Zero);
            }

            foreach (GeneratorDef generator in generators)
            {
                if (string.IsNullOrEmpty(generator.Id))
                {
                    continue;
                }

                var runtime = new GeneratorRuntime(generator)
                {
                    Unlocked = generator.UnlockCondition.UnlockedByDefault
                };
                _generators[generator.Id] = runtime;
            }
        }

        /// <inheritdoc />
        public void Initialize()
        {
            if (!_restoredFromSave)
            {
                foreach (ResourceDef resource in _resourceDefs.Values)
                {
                    _ledger.Set(resource.Id, resource.StartingAmount);
                    _lifetimeProduced.TryAdd(resource.Id, BigDouble.Zero);
                }

                foreach (GeneratorRuntime runtime in _generators.Values)
                {
                    runtime.Level = 0;
                    runtime.Unlocked = runtime.Def.UnlockCondition.UnlockedByDefault;
                    runtime.LastProductionPerSecond = BigDouble.Zero;
                }
            }

            _mapService.MapNodeUnlocked += OnMapNodeUnlocked;
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
            BigDouble delta = BigDouble.FromDouble(deltaTime);
            foreach (GeneratorRuntime runtime in _generators.Values)
            {
                if (!runtime.Unlocked || runtime.Level <= 0)
                {
                    runtime.LastProductionPerSecond = BigDouble.Zero;
                    continue;
                }

                BigDouble productionPerSecond = CalculateProductionPerSecond(runtime, runtime.Level);
                runtime.LastProductionPerSecond = productionPerSecond;
                BigDouble gain = productionPerSecond * delta;
                if (TryGetResourceDef(runtime.Def.ProducesResourceId, out ResourceDef? produced))
                {
                    AddResource(produced, gain);
                }
            }
        }

        /// <summary>
        /// Attempts to purchase generator levels using the provided multi-buy mode.
        /// </summary>
        public bool TryBuyGenerator(string generatorId, PurchaseMode mode)
        {
            if (!_generators.TryGetValue(generatorId, out GeneratorRuntime? runtime))
            {
                return false;
            }

            if (!runtime.Unlocked)
            {
                return false;
            }

            int quantity = ResolvePurchaseQuantity(runtime, mode);
            if (quantity <= 0)
            {
                return false;
            }

            BigDouble totalCost = CalculateTotalCost(runtime, quantity);
            if (!TrySpend(runtime.Def.RequiresResourceId, totalCost))
            {
                return false;
            }

            runtime.Level += quantity;
            runtime.LastKnownPurchaseQuantity = quantity;
            runtime.LastProductionPerSecond = CalculateProductionPerSecond(runtime, runtime.Level);
            return true;
        }

        /// <summary>
        /// Gets the aggregated cost for purchasing the supplied quantity.
        /// </summary>
        public BigDouble GetNextCost(string generatorId, int quantity)
        {
            if (!_generators.TryGetValue(generatorId, out GeneratorRuntime? runtime) || quantity <= 0)
            {
                return BigDouble.Zero;
            }

            return CalculateTotalCost(runtime, quantity);
        }

        /// <summary>
        /// Gets the total production per second for the supplied resource.
        /// </summary>
        public BigDouble GetProductionPerSec(string resourceId)
        {
            BigDouble total = BigDouble.Zero;
            foreach (GeneratorRuntime runtime in _generators.Values)
            {
                if (!runtime.Unlocked || runtime.Level <= 0 || runtime.Def.ProducesResourceId != resourceId)
                {
                    continue;
                }

                total += CalculateProductionPerSecond(runtime, runtime.Level);
            }

            return total;
        }

        /// <summary>
        /// Retrieves the current balance for the supplied resource.
        /// </summary>
        public BigDouble GetResourceAmount(string resourceId) => _ledger.Get(resourceId);

        /// <summary>
        /// Retrieves the current balance for the supplied resource definition.
        /// </summary>
        public BigDouble GetResourceAmount(ResourceDef resource) => GetResourceAmount(resource.Id);

        /// <summary>
        /// Enumerates configured resources.
        /// </summary>
        public IEnumerable<ResourceDef> EnumerateResources() => _resourceDefs.Values;

        /// <summary>
        /// Attempts to look up a resource definition by identifier.
        /// </summary>
        public bool TryGetResource(string resourceId, out ResourceDef resource) => _resourceDefs.TryGetValue(resourceId, out resource!);

        /// <summary>
        /// Enumerates generator runtimes for UI binding.
        /// </summary>
        public IEnumerable<GeneratorRuntime> EnumerateGenerators() => _generators.Values;

        /// <summary>
        /// Enumerates generator definitions for UI binding.
        /// </summary>
        public IEnumerable<GeneratorDef> EnumerateGeneratorDefinitions()
        {
            foreach (GeneratorRuntime runtime in _generators.Values)
            {
                yield return runtime.Def;
            }
        }

        /// <summary>
        /// Attempts to retrieve a generator runtime.
        /// </summary>
        public bool TryGetGeneratorRuntime(string generatorId, out GeneratorRuntime runtime) => _generators.TryGetValue(generatorId, out runtime!);

        /// <summary>
        /// Gets the affordable quantity for a purchase mode.
        /// </summary>
        public int GetAffordableQuantity(string generatorId, PurchaseMode mode)
        {
            if (!_generators.TryGetValue(generatorId, out GeneratorRuntime? runtime))
            {
                return 0;
            }

            return mode == PurchaseMode.Max ? CalculateMaxAffordableQuantity(runtime) : ResolvePurchaseQuantity(runtime, mode);
        }

        /// <summary>
        /// Attempts to build a snapshot describing current modifiers for a generator.
        /// </summary>
        public bool TryGetGeneratorSnapshot(string generatorId, out GeneratorSnapshot snapshot)
        {
            if (_generators.TryGetValue(generatorId, out GeneratorRuntime? runtime))
            {
                snapshot = BuildSnapshot(runtime);
                return true;
            }

            snapshot = default;
            return false;
        }

        /// <summary>
        /// Predicts the production before and after purchasing levels.
        /// </summary>
        public bool TryGetProductionProjection(string generatorId, int quantity, out BigDouble current, out BigDouble projected)
        {
            current = BigDouble.Zero;
            projected = BigDouble.Zero;

            if (!_generators.TryGetValue(generatorId, out GeneratorRuntime? runtime) || quantity <= 0)
            {
                return false;
            }

            current = CalculateProductionPerSecond(runtime, runtime.Level);
            projected = CalculateProductionPerSecond(runtime, runtime.Level + quantity);
            return true;
        }

        /// <summary>
        /// Applies offline progress based on stored production rates.
        /// </summary>
        public void ApplyOfflineProgress(double offlineSeconds)
        {
            if (offlineSeconds <= 0d)
            {
                return;
            }

            double effectiveSeconds = offlineSeconds * _offlineEfficiency;
            BigDouble delta = BigDouble.FromDouble(effectiveSeconds);
            foreach (GeneratorRuntime runtime in _generators.Values)
            {
                if (!runtime.Unlocked || runtime.Level <= 0)
                {
                    continue;
                }

                BigDouble productionPerSecond = CalculateProductionPerSecond(runtime, runtime.Level);
                BigDouble gain = productionPerSecond * delta;
                if (TryGetResourceDef(runtime.Def.ProducesResourceId, out ResourceDef? produced))
                {
                    AddResource(produced, gain);
                }
            }
        }

        /// <summary>
        /// Attempts to spend the supplied resource amount.
        /// </summary>
        public bool TrySpend(string resourceId, BigDouble amount) => _ledger.Spend(resourceId, amount);

        /// <summary>
        /// Attempts to spend the supplied resource amount.
        /// </summary>
        public bool TrySpend(ResourceDef resource, BigDouble amount) => TrySpend(resource.Id, amount);

        /// <summary>
        /// Adds the supplied amount to the specified resource.
        /// </summary>
        public void AddResource(ResourceDef resource, BigDouble amount)
        {
            if (amount.IsZero)
            {
                return;
            }

            BigDouble current = GetResourceAmount(resource.Id);
            BigDouble adjusted = ApplySoftCaps(resource, current, amount);
            _ledger.Add(resource.Id, adjusted);
            AccumulateLifetime(resource.Id, adjusted);
        }

        /// <summary>
        /// Gets the total lifetime production for a resource.
        /// </summary>
        public BigDouble GetLifetimeProduced(string resourceId)
        {
            return _lifetimeProduced.TryGetValue(resourceId, out BigDouble amount) ? amount : BigDouble.Zero;
        }

        /// <summary>
        /// Resets resource balances to their starting amounts.
        /// </summary>
        public void ResetResourcesToBase()
        {
            foreach (ResourceDef resource in _resourceDefs.Values)
            {
                _ledger.Set(resource.Id, resource.StartingAmount);
            }
        }

        /// <summary>
        /// Resets all generator levels and unlock state.
        /// </summary>
        public void ResetGenerators()
        {
            foreach (GeneratorRuntime runtime in _generators.Values)
            {
                runtime.Level = 0;
                runtime.Unlocked = runtime.Def.UnlockCondition.UnlockedByDefault;
                runtime.LastProductionPerSecond = BigDouble.Zero;
            }
        }

        /// <inheritdoc />
        public object CaptureState()
        {
            var save = new EconomySave
            {
                Resources = new List<ResourceState>(),
                Generators = new List<GeneratorState>(),
                Lifetime = new List<LifetimeState>()
            };

            foreach (ResourceDef resource in _resourceDefs.Values)
            {
                BigDouble amount = GetResourceAmount(resource.Id);
                save.Resources.Add(new ResourceState
                {
                    Id = resource.Id,
                    Mantissa = amount.Mantissa,
                    Exponent = amount.Exponent
                });

                BigDouble lifetime = GetLifetimeProduced(resource.Id);
                save.Lifetime.Add(new LifetimeState
                {
                    Id = resource.Id,
                    Mantissa = lifetime.Mantissa,
                    Exponent = lifetime.Exponent
                });
            }

            foreach (GeneratorRuntime runtime in _generators.Values)
            {
                save.Generators.Add(new GeneratorState
                {
                    Id = runtime.Def.Id,
                    Level = runtime.Level,
                    Unlocked = runtime.Unlocked
                });
            }

            return save;
        }

        /// <inheritdoc />
        public void RestoreState(object state)
        {
            if (state is not EconomySave save)
            {
                return;
            }

            foreach (ResourceState resourceState in save.Resources)
            {
                _ledger.Set(resourceState.Id, BigDouble.FromUnnormalized(resourceState.Mantissa, resourceState.Exponent));
            }

            _lifetimeProduced.Clear();
            foreach (LifetimeState lifetimeState in save.Lifetime)
            {
                _lifetimeProduced[lifetimeState.Id] = BigDouble.FromUnnormalized(lifetimeState.Mantissa, lifetimeState.Exponent);
            }

            foreach (GeneratorState generatorState in save.Generators)
            {
                if (_generators.TryGetValue(generatorState.Id, out GeneratorRuntime? runtime))
                {
                    runtime.Level = generatorState.Level;
                    runtime.Unlocked = generatorState.Unlocked;
                    runtime.LastProductionPerSecond = CalculateProductionPerSecond(runtime, runtime.Level);
                }
            }

            _restoredFromSave = true;
        }

        private void OnEventMultiplierChanged(double multiplier)
        {
            _eventMultiplier = multiplier <= 0d ? 1d : multiplier;
        }

        private void OnMapNodeUnlocked(MapNodeDef node)
        {
            UpdateUnlockStates();
        }

        private void UpdateUnlockStates()
        {
            foreach (GeneratorRuntime runtime in _generators.Values)
            {
                if (runtime.Unlocked)
                {
                    continue;
                }

                GeneratorUnlockCondition condition = runtime.Def.UnlockCondition;
                bool resourceGateMet = !condition.HasResourceGate || GetResourceAmount(condition.RequiredResourceId) >= BigDouble.FromDouble(condition.RequiredResourceAmount);
                bool mapGateMet = !condition.HasMapGate || _mapService.IsUnlocked(condition.RequiredMapNodeId);
                runtime.Unlocked = resourceGateMet && mapGateMet;
            }
        }

        private BigDouble CalculateProductionPerSecond(GeneratorRuntime runtime, int assumedLevel)
        {
            if (assumedLevel <= 0)
            {
                return BigDouble.Zero;
            }

            double basePerSecond = runtime.Def.BaseProductionPerSecond * assumedLevel;
            double additive = 0d;
            double multiplicative = 1d;
            double exponential = 1d;

            foreach (string tag in EnumerateEffectiveTags(runtime.Def))
            {
                if (_upgradeService != null)
                {
                    additive += _upgradeService.GetModifierForTag(tag, UpgradeEffectType.Additive);
                    multiplicative *= _upgradeService.GetModifierForTag(tag, UpgradeEffectType.Multiplicative);
                    exponential *= _upgradeService.GetModifierForTag(tag, UpgradeEffectType.Exponential);
                }
            }

            double externalMultiplier = _mapService.GetGlobalMultiplier() * _eventMultiplier;
            double additiveFactor = Math.Max(0d, 1d + additive);
            double multiplicativeFactor = Math.Max(0d, multiplicative);
            double exponentialFactor = Math.Max(0d, exponential);

            BigDouble result = BigDouble.FromDouble(basePerSecond);
            result *= BigDouble.FromDouble(exponentialFactor);
            result *= BigDouble.FromDouble(additiveFactor);
            result *= BigDouble.FromDouble(multiplicativeFactor);
            result *= BigDouble.FromDouble(externalMultiplier);
            return result;
        }

        private GeneratorSnapshot BuildSnapshot(GeneratorRuntime runtime)
        {
            double basePerSecond = runtime.Level > 0 ? runtime.Def.BaseProductionPerSecond * runtime.Level : 0d;
            double additive = 0d;
            double multiplicative = 1d;
            double exponential = 1d;

            foreach (string tag in EnumerateEffectiveTags(runtime.Def))
            {
                if (_upgradeService != null)
                {
                    additive += _upgradeService.GetModifierForTag(tag, UpgradeEffectType.Additive);
                    multiplicative *= _upgradeService.GetModifierForTag(tag, UpgradeEffectType.Multiplicative);
                    exponential *= _upgradeService.GetModifierForTag(tag, UpgradeEffectType.Exponential);
                }
            }

            double externalMultiplier = _mapService.GetGlobalMultiplier() * _eventMultiplier;
            BigDouble production = runtime.Level > 0 ? CalculateProductionPerSecond(runtime, runtime.Level) : BigDouble.Zero;
            return new GeneratorSnapshot(runtime, basePerSecond, Math.Max(0d, 1d + additive), Math.Max(0d, multiplicative), Math.Max(0d, exponential), externalMultiplier, production);
        }

        private BigDouble CalculateTotalCost(GeneratorRuntime runtime, int quantity)
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

        private int ResolvePurchaseQuantity(GeneratorRuntime runtime, PurchaseMode mode)
        {
            return mode switch
            {
                PurchaseMode.X10 => 10,
                PurchaseMode.X100 => 100,
                PurchaseMode.Max => CalculateMaxAffordableQuantity(runtime),
                _ => 1
            };
        }

        private int CalculateMaxAffordableQuantity(GeneratorRuntime runtime)
        {
            BigDouble available = GetResourceAmount(runtime.Def.RequiresResourceId);
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

            double logMultiplier = Math.Log10(multiplier);
            double n = Math.Floor(ratio.Log10() / logMultiplier);
            int quantity = Math.Max(0, (int)n);
            while (quantity > 0 && CalculateTotalCost(runtime, quantity) > available)
            {
                quantity--;
            }

            return quantity;
        }

        private IEnumerable<string> EnumerateEffectiveTags(GeneratorDef def)
        {
            yield return AllGeneratorsTag;
            if (!string.IsNullOrEmpty(def.Id))
            {
                yield return def.Id;
            }

            if (!string.IsNullOrEmpty(def.ProducesResourceId))
            {
                yield return def.ProducesResourceId;
            }

            foreach (string tag in def.Tags)
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    yield return tag;
                }
            }
        }

        private BigDouble ApplySoftCaps(ResourceDef def, BigDouble current, BigDouble delta)
        {
            BigDouble adjusted = delta;
            foreach (ResourceDef.SoftCapThreshold threshold in def.SoftCapThresholds)
            {
                if (current >= threshold.Amount)
                {
                    adjusted = adjusted.Pow(threshold.Exponent);
                }
            }

            return adjusted;
        }

        private void AccumulateLifetime(string resourceId, BigDouble amount)
        {
            if (amount.IsZero)
            {
                return;
            }

            if (_lifetimeProduced.TryGetValue(resourceId, out BigDouble existing))
            {
                _lifetimeProduced[resourceId] = existing + amount;
            }
            else
            {
                _lifetimeProduced[resourceId] = amount;
            }
        }

        private bool TryGetResourceDef(string resourceId, out ResourceDef? def) => _resourceDefs.TryGetValue(resourceId, out def);

        [Serializable]
        private sealed class EconomySave
        {
            public List<ResourceState> Resources = new();
            public List<GeneratorState> Generators = new();
            public List<LifetimeState> Lifetime = new();
        }

        [Serializable]
        private sealed class ResourceState
        {
            public string Id = string.Empty;
            public double Mantissa;
            public int Exponent;
        }

        [Serializable]
        private sealed class GeneratorState
        {
            public string Id = string.Empty;
            public int Level;
            public bool Unlocked;
        }

        [Serializable]
        private sealed class LifetimeState
        {
            public string Id = string.Empty;
            public double Mantissa;
            public int Exponent;
        }

        /// <summary>
        /// Runtime data for a generator instance.
        /// </summary>
        public sealed class GeneratorRuntime
        {
            internal GeneratorRuntime(GeneratorDef def)
            {
                Def = def;
            }

            /// <summary>
            /// Gets the backing generator definition.
            /// </summary>
            public GeneratorDef Def { get; }

            /// <summary>
            /// Gets or sets the current level.
            /// </summary>
            public int Level { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the generator is unlocked.
            /// </summary>
            public bool Unlocked { get; set; }

            /// <summary>
            /// Gets or sets the last computed production rate.
            /// </summary>
            public BigDouble LastProductionPerSecond { get; set; } = BigDouble.Zero;

            /// <summary>
            /// Gets or sets the last purchased quantity for UI feedback.
            /// </summary>
            public int LastKnownPurchaseQuantity { get; set; }
        }

        /// <summary>
        /// Snapshot describing production modifiers for a generator.
        /// </summary>
        public readonly struct GeneratorSnapshot
        {
            internal GeneratorSnapshot(GeneratorRuntime generator, double basePerSecond, double additiveFactor, double multiplicativeFactor, double exponentialFactor, double externalMultiplier, BigDouble production)
            {
                Generator = generator;
                BasePerSecond = basePerSecond;
                AdditiveFactor = additiveFactor;
                MultiplicativeFactor = multiplicativeFactor;
                ExponentialFactor = exponentialFactor;
                ExternalMultiplier = externalMultiplier;
                Production = production;
            }

            /// <summary>
            /// Gets the generator runtime associated with the snapshot.
            /// </summary>
            public GeneratorRuntime Generator { get; }

            /// <summary>
            /// Gets the base production per second (level scaled, before modifiers).
            /// </summary>
            public double BasePerSecond { get; }

            /// <summary>
            /// Gets the additive factor (1 + additive bonuses).
            /// </summary>
            public double AdditiveFactor { get; }

            /// <summary>
            /// Gets the multiplicative factor applied after additive bonuses.
            /// </summary>
            public double MultiplicativeFactor { get; }

            /// <summary>
            /// Gets the exponential factor applied to the base production.
            /// </summary>
            public double ExponentialFactor { get; }

            /// <summary>
            /// Gets the combined event/map multiplier.
            /// </summary>
            public double ExternalMultiplier { get; }

            /// <summary>
            /// Gets the final production per second.
            /// </summary>
            public BigDouble Production { get; }
        }
    }
}
