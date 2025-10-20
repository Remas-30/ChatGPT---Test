using System;
using GalacticExpansion.Core;
using UnityEngine;

namespace GalacticExpansion.UI.Nav
{
    /// <summary>
    /// Centralizes navigation between primary UI panels.
    /// </summary>
    public sealed class UIRouter : MonoBehaviour
    {
        [SerializeField] private TabController tabController = null!;
        [SerializeField] private string generatorsTabId = "Generators";
        [SerializeField] private string upgradesTabId = "Upgrades";
        [SerializeField] private string prestigeTabId = "Prestige";
        [SerializeField] private string mapTabId = "Map";
        [SerializeField] private string helpTabId = "Help";

        /// <summary>
        /// Navigates to the generators tab.
        /// </summary>
        public void OpenGenerators() => Activate(generatorsTabId);

        /// <summary>
        /// Navigates to the upgrades tab.
        /// </summary>
        public void OpenUpgrades() => Activate(upgradesTabId);

        /// <summary>
        /// Navigates to the prestige tab.
        /// </summary>
        public void OpenPrestige() => Activate(prestigeTabId);

        /// <summary>
        /// Navigates to the map tab.
        /// </summary>
        public void OpenMap() => Activate(mapTabId);

        /// <summary>
        /// Navigates to the help tab.
        /// </summary>
        public void OpenHelp() => Activate(helpTabId);

        /// <summary>
        /// Handles deep links emitted from help page markup.
        /// </summary>
        public void Navigate(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return;
            }

            if (uri.Equals("app://open/generators", StringComparison.OrdinalIgnoreCase))
            {
                OpenGenerators();
            }
            else if (uri.Equals("app://open/upgrades", StringComparison.OrdinalIgnoreCase))
            {
                OpenUpgrades();
            }
            else if (uri.Equals("app://open/prestige", StringComparison.OrdinalIgnoreCase))
            {
                OpenPrestige();
            }
            else if (uri.Equals("app://open/events", StringComparison.OrdinalIgnoreCase))
            {
                OpenHelp();
            }
            else if (uri.Equals("app://open/map", StringComparison.OrdinalIgnoreCase))
            {
                OpenMap();
            }
            else if (uri.Equals("app://open/help", StringComparison.OrdinalIgnoreCase))
            {
                OpenHelp();
            }
        }

        private void Activate(string tabId)
        {
            if (tabController == null)
            {
                return;
            }

            tabController.ActivateTabById(tabId);
        }
    }
}
