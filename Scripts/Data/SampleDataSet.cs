using System;
using System.Collections.Generic;
using UnityEngine;

namespace GalacticExpansion.Data
{
    /// <summary>
    /// Provides a catalog of sample data used for documentation and quick prototyping.
    /// </summary>
    [CreateAssetMenu(menuName = "Galactic Expansion/Sample Data Set", fileName = "SampleDataSet")]
    public sealed class SampleDataSet : ScriptableObject
    {
        [SerializeField] private List<ResourceSample> resources = new();
        [SerializeField] private List<GeneratorSample> generators = new();
        [SerializeField] private List<UpgradeSample> upgrades = new();
        [SerializeField] private List<PrestigeSample> prestiges = new();
        [SerializeField] private List<EventSample> events = new();

        public IReadOnlyList<ResourceSample> Resources => resources;
        public IReadOnlyList<GeneratorSample> Generators => generators;
        public IReadOnlyList<UpgradeSample> Upgrades => upgrades;
        public IReadOnlyList<PrestigeSample> Prestiges => prestiges;
        public IReadOnlyList<EventSample> Events => events;

        [Serializable]
        public struct ResourceSample
        {
            public string Id;
            public string Name;
            public double BaseAmount;
            public double SoftCapThreshold;
            public float SoftCapExponent;
        }

        [Serializable]
        public struct GeneratorSample
        {
            public string Id;
            public string Name;
            public string OutputResourceId;
            public double BaseProduction;
            public double BaseCost;
            public float CostMultiplier;
        }

        [Serializable]
        public struct UpgradeSample
        {
            public string Id;
            public string Name;
            public string Description;
            public string CostResourceId;
            public double CostAmount;
            public string[] TargetTags;
            public float Multiplier;
            public double AdditiveBonus;
        }

        [Serializable]
        public struct PrestigeSample
        {
            public string Id;
            public string DisplayName;
            public PrestigeTier Tier;
            public string RequiredResourceId;
            public double RequiredAmount;
            public double BaseReward;
            public float RewardExponent;
        }

        [Serializable]
        public struct EventSample
        {
            public string Id;
            public string DisplayName;
            public EventType Type;
            public float DurationSeconds;
            public float ProductionMultiplier;
        }
    }
}
