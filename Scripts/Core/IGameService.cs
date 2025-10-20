using System;

namespace GalacticExpansion.Core
{
    /// <summary>
    /// Base interface for all services participating in the game lifecycle.
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// Raised when the service has completed initialization.
        /// </summary>
        event Action ServiceReady;

        /// <summary>
        /// Initializes the service and prepares it for runtime use.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Performs per-frame or per-tick updates when required.
        /// </summary>
        /// <param name="deltaTime">The delta time in seconds.</param>
        void Tick(double deltaTime);
    }
}
