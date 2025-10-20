namespace GalacticExpansion.Core
{
    /// <summary>
    /// Provides serialization hooks for persistent save data.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique key identifying the saveable object in the save file.
        /// </summary>
        string SaveKey { get; }

        /// <summary>
        /// Creates a serializable object representing the runtime state.
        /// </summary>
        object CaptureState();

        /// <summary>
        /// Restores state from the provided serialized object.
        /// </summary>
        /// <param name="state">The deserialized state object.</param>
        void RestoreState(object state);
    }
}
