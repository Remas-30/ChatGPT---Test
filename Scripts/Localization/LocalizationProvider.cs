using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GalacticExpansion.Localization
{
    /// <summary>
    /// Minimal localization provider that loads TextAssets from the Resources folder.
    /// </summary>
    public sealed class LocalizationProvider : MonoBehaviour
    {
        private static LocalizationProvider? _instance;
        private readonly Dictionary<string, string> _entries = new();

        [SerializeField] private string locale = "en";
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadLocale(locale);
        }

        /// <summary>
        /// Attempts to fetch a localized string for the provided key.
        /// </summary>
        public static bool TryGet(string key, out string value)
        {
            if (_instance == null)
            {
                value = string.Empty;
                return false;
            }

            return _instance._entries.TryGetValue(key, out value!);
        }

        /// <summary>
        /// Loads all TextAssets for the specified locale into memory.
        /// </summary>
        private void LoadLocale(string localeCode)
        {
            _entries.Clear();
            TextAsset[] assets = Resources.LoadAll<TextAsset>($"Localization/{localeCode}");
            foreach (TextAsset asset in assets)
            {
                if (asset == null)
                {
                    continue;
                }

                string key = asset.name.ToUpperInvariant();
                string text = asset.text;

                using StringReader reader = new(text);
                string? firstLine = reader.ReadLine();
                if (firstLine != null && firstLine.StartsWith("key:", StringComparison.OrdinalIgnoreCase))
                {
                    key = firstLine.Substring(4).Trim();
                    text = reader.ReadToEnd();
                }

                _entries[key] = text.Trim();
            }
        }
    }
}
