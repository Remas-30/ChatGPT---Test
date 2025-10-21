using System;
using System.Collections.Generic;
using GalacticExpansion.Core;
using GalacticExpansion.Data;
using UnityEngine;

namespace GalacticExpansion.Services
{
    /// <summary>
    /// Handles timed global events that provide temporary boosts.
    /// </summary>
    public sealed class EventService : IGameService, ISaveable
    {
        private readonly List<EventDef> _events = new();
        private readonly System.Random _random = new();
        private float _activeTimer;
        private float _currentMultiplier = 1f;
        private EventDef? _activeEvent;
        private bool _initialized;

        public event Action? ServiceReady;
        public event Action<float>? EventMultiplierChanged;
        public event Action<EventDef>? EventStarted;
        public event Action<EventDef>? EventEnded;

        public EventService(EventDef[] events)
        {
            _events.AddRange(events);
        }

        public string SaveKey => "events";

        public void Initialize()
        {
            _initialized = true;
            ServiceReady?.Invoke();
        }

        public void Tick(double deltaTime)
        {
            if (!_initialized)
            {
                return;
            }

            if (_activeEvent != null)
            {
                _activeTimer -= (float)deltaTime;
                if (_activeTimer <= 0f)
                {
                    EndActiveEvent();
                }
            }
            else if (_events.Count > 0)
            {
                // Random chance per tick for simplicity.
                if (_random.NextDouble() < 0.01d)
                {
                    StartRandomEvent();
                }
            }
        }

        public float GetCurrentMultiplier() => _currentMultiplier;

        public object CaptureState()
        {
            return new EventSave
            {
                ActiveEventId = _activeEvent?.Id,
                RemainingSeconds = _activeTimer,
                CurrentMultiplier = _currentMultiplier
            };
        }

        public void RestoreState(object state)
        {
            if (state is not EventSave save)
            {
                return;
            }

            if (!string.IsNullOrEmpty(save.ActiveEventId))
            {
                _activeEvent = _events.Find(e => e.Id == save.ActiveEventId);
                if (_activeEvent != null)
                {
                    _activeTimer = save.RemainingSeconds;
                    _currentMultiplier = save.CurrentMultiplier;
                    EventMultiplierChanged?.Invoke(_currentMultiplier);
                }
            }
        }

        private void StartRandomEvent()
        {
            int index = _random.Next(0, _events.Count);
            _activeEvent = _events[index];
            _activeTimer = _activeEvent.DurationSeconds;
            _currentMultiplier = _activeEvent.ProductionMultiplier;
            EventStarted?.Invoke(_activeEvent);
            EventMultiplierChanged?.Invoke(_currentMultiplier);
        }

        private void EndActiveEvent()
        {
            if (_activeEvent != null)
            {
                EventEnded?.Invoke(_activeEvent);
            }

            _activeEvent = null;
            _activeTimer = 0f;
            _currentMultiplier = 1f;
            EventMultiplierChanged?.Invoke(_currentMultiplier);
        }

        [Serializable]
        private sealed class EventSave
        {
            public string ActiveEventId = string.Empty;
            public float RemainingSeconds;
            public float CurrentMultiplier = 1f;
        }
    }
}
