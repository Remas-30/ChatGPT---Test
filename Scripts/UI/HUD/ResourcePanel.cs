using System;
using System.Collections.Generic;
using GalacticExpansion.Core;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using TMPro;
using UnityEngine;

namespace GalacticExpansion.UI.HUD
{
    /// <summary>
    /// Builds and updates the HUD resource list at runtime.
    /// </summary>
    public sealed class ResourcePanel : MonoBehaviour
    {
        [SerializeField] private ResourceRow rowPrefab = null!;
        [SerializeField] private RectTransform contentRoot = null!;
        [SerializeField] private ResourceDef[] orderedResources = Array.Empty<ResourceDef>();
        [Header("Meta Currency")]
        [SerializeField] private GameObject warpCoreGroup = null!;
        [SerializeField] private TextMeshProUGUI warpCoreLabel = null!;
        [SerializeField] private string warpCurrencyId = "WarpCores";

        private readonly List<ResourceRow> _rows = new();
        private EconomyService _economy = null!;
        private MetaCurrencyService _meta = null!;

        private void Awake()
        {
            _economy = ServiceLocator.Get<EconomyService>();
            _meta = ServiceLocator.Get<MetaCurrencyService>();
            if (rowPrefab != null)
            {
                rowPrefab.gameObject.SetActive(false);
            }

            BuildRows();
        }

        private void Update()
        {
            foreach (ResourceRow row in _rows)
            {
                row.Refresh();
            }

            UpdateWarpIndicator();
        }

        private void BuildRows()
        {
            IEnumerable<ResourceDef> resources = orderedResources.Length > 0 ? orderedResources : _economy.EnumerateResources();
            foreach (ResourceDef resource in resources)
            {
                if (rowPrefab == null || contentRoot == null)
                {
                    break;
                }

                ResourceRow row = Instantiate(rowPrefab, contentRoot);
                row.gameObject.SetActive(true);
                row.Initialize(resource, _economy);
                _rows.Add(row);
            }
        }

        private void UpdateWarpIndicator()
        {
            if (warpCoreGroup == null || warpCoreLabel == null || _meta == null)
            {
                return;
            }

            BigDouble amount = _meta.GetCurrency(warpCurrencyId);
            bool visible = amount > BigDouble.Zero;
            if (warpCoreGroup.activeSelf != visible)
            {
                warpCoreGroup.SetActive(visible);
            }

            if (visible)
            {
                warpCoreLabel.text = amount.ToShortString(3, BigDoubleFormat.Scientific);
            }
        }
    }
}
