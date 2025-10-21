using System;
using System.Collections.Generic;
using GalacticExpansion.Core;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using UnityEngine;

namespace GalacticExpansion.UI.Upgrades
{
    /// <summary>
    /// Builds the upgrade list and keeps it in sync with runtime services.
    /// </summary>
    public sealed class UpgradesPanel : MonoBehaviour
    {
        [SerializeField] private UpgradeRow rowPrefab = null!;
        [SerializeField] private RectTransform contentRoot = null!;
        [SerializeField] private UpgradeDef[] orderedUpgrades = Array.Empty<UpgradeDef>();

        private readonly List<UpgradeRow> _rows = new();
        private UpgradeService _upgradeService = null!;
        private EconomyService _economyService = null!;
        private MetaCurrencyService _metaService = null!;

        private void Awake()
        {
            _upgradeService = ServiceLocator.Get<UpgradeService>();
            _economyService = ServiceLocator.Get<EconomyService>();
            _metaService = ServiceLocator.Get<MetaCurrencyService>();

            if (rowPrefab != null)
            {
                rowPrefab.gameObject.SetActive(false);
            }

            BuildRows();
        }

        private void Update()
        {
            foreach (UpgradeRow row in _rows)
            {
                row.Refresh();
            }
        }

        private void BuildRows()
        {
            IEnumerable<UpgradeDef> upgrades = orderedUpgrades.Length > 0 ? orderedUpgrades : _upgradeService.EnumerateUpgradesDefinitions();
            foreach (UpgradeDef upgrade in upgrades)
            {
                if (rowPrefab == null || contentRoot == null)
                {
                    break;
                }

                if (upgrade == null)
                {
                    continue;
                }

                UpgradeRow row = Instantiate(rowPrefab, contentRoot);
                row.gameObject.SetActive(true);
                row.Initialize(upgrade, _upgradeService, _economyService, _metaService);
                _rows.Add(row);
            }
        }
    }
}
