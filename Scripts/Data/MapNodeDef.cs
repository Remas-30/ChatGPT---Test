using System.Collections.Generic;
using UnityEngine;

namespace GalacticExpansion.Data
{
    /// <summary>
    /// Defines a map node representing a star system or galaxy region.
    /// </summary>
    [CreateAssetMenu(menuName = "Galactic Expansion/Map Node", fileName = "MapNode")]
    public sealed class MapNodeDef : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private Sprite illustration = null!;

        [Header("Requirements")]
        [SerializeField] private List<string> requiredResources = new();
        [SerializeField] private List<double> requiredAmounts = new();
        [SerializeField] private List<MapNodeDef> requiredNodes = new();

        [Header("Bonuses")]
        [SerializeField] private List<string> grantedTags = new();
        [SerializeField] private float productionMultiplier = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Illustration => illustration;
        public IReadOnlyList<string> RequiredResources => requiredResources;
        public IReadOnlyList<double> RequiredAmounts => requiredAmounts;
        public IReadOnlyList<MapNodeDef> RequiredNodes => requiredNodes;
        public IReadOnlyList<string> GrantedTags => grantedTags;
        public float ProductionMultiplier => productionMultiplier;
    }
}
