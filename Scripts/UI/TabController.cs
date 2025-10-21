using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GalacticExpansion.UI
{
    /// <summary>
    /// Simple tab controller toggling canvases.
    /// </summary>
    public sealed class TabController : MonoBehaviour
    {
        [SerializeField] private List<Button> tabButtons = new();
        [SerializeField] private List<GameObject> tabPanels = new();
        [SerializeField] private List<string> tabIds = new();

        private int _currentIndex;

        private void Awake()
        {
            for (int i = 0; i < tabButtons.Count; i++)
            {
                int index = i;
                tabButtons[i].onClick.AddListener(() => ActivateTab(index));
            }

            ActivateTab(0);
        }

        /// <summary>
        /// Activates a tab by index.
        /// </summary>
        public void ActivateTab(int index)
        {
            if (tabPanels.Count == 0)
            {
                return;
            }

            _currentIndex = Mathf.Clamp(index, 0, tabPanels.Count - 1);
            for (int i = 0; i < tabPanels.Count; i++)
            {
                bool active = i == _currentIndex;
                if (tabPanels[i] != null)
                {
                    tabPanels[i].SetActive(active);
                }
            }
        }

        /// <summary>
        /// Activates a tab using its identifier.
        /// </summary>
        public void ActivateTabById(string tabId)
        {
            if (string.IsNullOrEmpty(tabId))
            {
                return;
            }

            int index = tabIds.IndexOf(tabId);
            if (index >= 0)
            {
                ActivateTab(index);
            }
        }
    }
}
