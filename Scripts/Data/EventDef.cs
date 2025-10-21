using UnityEngine;

namespace GalacticExpansion.Data
{
    public enum EventType
    {
        CometShower,
        WormholeSurge,
        SupernovaRemnant
    }

    /// <summary>
    /// Defines temporary events that grant production multipliers.
    /// </summary>
    [CreateAssetMenu(menuName = "Galactic Expansion/Event", fileName = "Event")]
    public sealed class EventDef : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private EventType type = EventType.CometShower;
        [SerializeField] private float durationSeconds = 60f;
        [SerializeField] private float productionMultiplier = 2f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public EventType Type => type;
        public float DurationSeconds => durationSeconds;
        public float ProductionMultiplier => productionMultiplier;
    }
}
