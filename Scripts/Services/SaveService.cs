using System;
using System.Collections.Generic;
using System.IO;
using GalacticExpansion.Core;
using UnityEngine;

namespace GalacticExpansion.Services
{
    /// <summary>
    /// Handles serialization of persistent save data using JSON and applies offline progress on load.
    /// </summary>
    public sealed class SaveService : IGameService
    {
        private readonly Dictionary<string, ISaveable> _saveables = new();
        private readonly string _fileName;
        private readonly string _version;
        private readonly TimeService _timeService;
        private readonly EconomyService _economyService;
        private float _autosaveTimer;
        private bool _initialized;
        private bool _hasLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveService"/> class.
        /// </summary>
        public SaveService(string fileName, string version, TimeService timeService, EconomyService economyService)
        {
            _fileName = fileName;
            _version = version;
            _timeService = timeService;
            _economyService = economyService;
        }

        /// <summary>
        /// Raised when initialization completes.
        /// </summary>
        public event Action? ServiceReady;

        /// <summary>
        /// Registers a saveable instance with the service.
        /// </summary>
        public void RegisterSaveable(ISaveable saveable)
        {
            if (saveable == null)
            {
                throw new ArgumentNullException(nameof(saveable));
            }

            _saveables[saveable.SaveKey] = saveable;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            if (!_hasLoaded)
            {
                Load();
            }

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

            _autosaveTimer += (float)deltaTime;
            if (_autosaveTimer >= 30f)
            {
                Save();
                _autosaveTimer = 0f;
            }
        }

        /// <summary>
        /// Saves all registered state to disk.
        /// </summary>
        public void Save()
        {
            if (_saveables.Count == 0)
            {
                return;
            }

            SaveFile file = new()
            {
                Version = _version,
                TimestampUtc = DateTime.UtcNow.ToString("o")
            };

            foreach (var pair in _saveables)
            {
                object state = pair.Value.CaptureState();
                if (state == null)
                {
                    continue;
                }

                Type stateType = state.GetType();
                string jsonState = JsonUtility.ToJson(state);
                file.Entries.Add(new SaveEntry
                {
                    Key = pair.Key,
                    TypeName = stateType.AssemblyQualifiedName ?? stateType.FullName ?? string.Empty,
                    Json = jsonState
                });
            }

            string path = GetSaveFilePath();
            string json = JsonUtility.ToJson(file, true);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Loads the save file from disk and restores registered state.
        /// </summary>
        public void Load()
        {
            string path = GetSaveFilePath();
            if (!File.Exists(path))
            {
                return;
            }

            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            SaveFile? file = JsonUtility.FromJson<SaveFile>(json);
            if (file == null)
            {
                return;
            }

            RunMigrations(file);
            _hasLoaded = true;
            foreach (SaveEntry entry in file.Entries)
            {
                if (!_saveables.TryGetValue(entry.Key, out ISaveable saveable))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(entry.TypeName))
                {
                    continue;
                }

                Type? stateType = Type.GetType(entry.TypeName);
                if (stateType == null)
                {
                    Debug.LogWarning($"SaveService: Unable to resolve state type '{entry.TypeName}'.");
                    continue;
                }

                object? instance = Activator.CreateInstance(stateType);
                if (instance == null)
                {
                    continue;
                }

                JsonUtility.FromJsonOverwrite(entry.Json, instance);
                saveable.RestoreState(instance);
            }

        }

        /// <summary>
        /// Deletes the current save file.
        /// </summary>
        public void DeleteSave()
        {
            string path = GetSaveFilePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Applies offline progress based on the restored timestamp.
        /// </summary>
        public void ApplyOfflineProgress()
        {
            double offlineSeconds = _timeService.OfflineSeconds;
            if (offlineSeconds > 0d)
            {
                _economyService.ApplyOfflineProgress(offlineSeconds);
                _timeService.ConsumeOffline();
            }
        }

        private string GetSaveFilePath() => Path.Combine(Application.persistentDataPath, _fileName);

        private void RunMigrations(SaveFile file)
        {
            if (string.IsNullOrEmpty(file.Version))
            {
                file.Version = "1";
            }

            if (file.Version != _version)
            {
                Debug.Log($"SaveService: upgrading save from version {file.Version} to {_version}.");
                file.Version = _version;
            }
        }

        [Serializable]
        private sealed class SaveFile
        {
            public string Version = string.Empty;
            public string TimestampUtc = string.Empty;
            public List<SaveEntry> Entries = new();
        }

        [Serializable]
        private sealed class SaveEntry
        {
            public string Key = string.Empty;
            public string TypeName = string.Empty;
            public string Json = string.Empty;
        }
    }
}
