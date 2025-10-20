using System;
using System.Collections.Generic;
using GalacticExpansion.Core;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using UnityEngine;

namespace GalacticExpansion.UI.Generators
{
    /// <summary>
    /// Builds the generator list UI and keeps it in sync with the economy service.
    /// </summary>
    public sealed class GeneratorsPanel : MonoBehaviour
    {
        [SerializeField] private GeneratorRow rowPrefab = null!;
        [SerializeField] private RectTransform contentRoot = null!;
        [SerializeField] private GeneratorDef[] orderedGenerators = Array.Empty<GeneratorDef>();

        private readonly List<GeneratorRow> _rows = new();
        private EconomyService _economy = null!;

        private void Awake()
        {
            _economy = ServiceLocator.Get<EconomyService>();
            if (rowPrefab != null)
            {
                rowPrefab.gameObject.SetActive(false);
            }

            BuildRows();
        }

        private void Update()
        {
            foreach (GeneratorRow row in _rows)
            {
                row.Refresh();
            }
        }

        private void BuildRows()
        {
            IEnumerable<GeneratorDef> generators = orderedGenerators.Length > 0 ? orderedGenerators : _economy.EnumerateGeneratorDefinitions();
            foreach (GeneratorDef generator in generators)
            {
                if (rowPrefab == null || contentRoot == null)
                {
                    break;
                }

                GeneratorRow row = Instantiate(rowPrefab, contentRoot);
                row.gameObject.SetActive(true);
                row.Initialize(generator, _economy);
                _rows.Add(row);
            }
        }
    }
}
