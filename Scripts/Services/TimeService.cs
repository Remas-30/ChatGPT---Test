using System;
using System.Globalization;
using GalacticExpansion.Core;

namespace GalacticExpansion.Services
{
    /// <summary>
    /// Tracks runtime delta time and offline time for idle progression.
    /// </summary>
    public sealed class TimeService : IGameService, ISaveable
    {
        private readonly double _maxOfflineSeconds;
        private DateTime _lastTickTimeUtc;
        private double _deltaTime;
        private double _offlineSeconds;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeService"/> class.
        /// </summary>
        /// <param name="maxOfflineHours">Maximum hours of offline time that can be rewarded.</param>
        public TimeService(double maxOfflineHours)
        {
            _maxOfflineSeconds = Math.Max(0d, maxOfflineHours * 3600d);
        }

        /// <summary>
        /// Raised when initialization completes.
        /// </summary>
        public event Action? ServiceReady;

        /// <inheritdoc />
        public string SaveKey => "time";

        /// <summary>
        /// Gets the delta time computed during the last update tick.
        /// </summary>
        public double DeltaTime => _deltaTime;

        /// <summary>
        /// Gets the accumulated offline seconds calculated on load.
        /// </summary>
        public double OfflineSeconds => _offlineSeconds;

        /// <summary>
        /// Resets the offline timer once the value has been consumed.
        /// </summary>
        public void ConsumeOffline() => _offlineSeconds = 0d;

        /// <inheritdoc />
        public void Initialize()
        {
            _lastTickTimeUtc = DateTime.UtcNow;
            _deltaTime = 0d;
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

            DateTime now = DateTime.UtcNow;
            _deltaTime = (now - _lastTickTimeUtc).TotalSeconds;
            _lastTickTimeUtc = now;
        }

        /// <inheritdoc />
        public object CaptureState()
        {
            return new TimeSaveData
            {
                LastUtcTimestamp = _lastTickTimeUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
            };
        }

        /// <inheritdoc />
        public void RestoreState(object state)
        {
            if (state is TimeSaveData save && !string.IsNullOrEmpty(save.LastUtcTimestamp))
            {
                if (DateTime.TryParse(save.LastUtcTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime last))
                {
                    DateTime now = DateTime.UtcNow;
                    double seconds = Math.Max(0d, (now - last).TotalSeconds);
                    _offlineSeconds = Math.Min(_maxOfflineSeconds, seconds);
                }
            }

            _lastTickTimeUtc = DateTime.UtcNow;
        }

        [Serializable]
        private struct TimeSaveData
        {
            public string LastUtcTimestamp;
        }
    }
}
