using GalacticExpansion.Core;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GalacticExpansion.UI.HUD
{
    /// <summary>
    /// Displays a single resource entry in the HUD list.
    /// </summary>
    public sealed class ResourceRow : MonoBehaviour
    {
        [SerializeField] private Image iconImage = null!;
        [SerializeField] private TextMeshProUGUI nameLabel = null!;
        [SerializeField] private TextMeshProUGUI valueLabel = null!;
        [SerializeField] private TextMeshProUGUI perSecondLabel = null!;

        private ResourceDef _resource = null!;
        private EconomyService _economy = null!;

        /// <summary>
        /// Initializes the row with a resource definition and economy service.
        /// </summary>
        public void Initialize(ResourceDef resource, EconomyService economy)
        {
            _resource = resource;
            _economy = economy;

            if (iconImage != null)
            {
                iconImage.sprite = resource.Icon;
            }

            if (nameLabel != null)
            {
                nameLabel.text = resource.DisplayName;
            }

            Refresh();
        }

        /// <summary>
        /// Updates the displayed values.
        /// </summary>
        public void Refresh()
        {
            if (_resource == null || _economy == null)
            {
                return;
            }

            BigDouble amount = _economy.GetResourceAmount(_resource);
            BigDouble perSecond = _economy.GetProductionPerSec(_resource.Id);

            if (valueLabel != null)
            {
                valueLabel.text = Format(amount, _resource.DisplayFormat);
            }

            if (perSecondLabel != null)
            {
                string formatted = Format(perSecond, _resource.DisplayFormat);
                perSecondLabel.text = $"+{formatted}/s";
            }
        }

        private static string Format(BigDouble value, ResourceDisplayFormat displayFormat)
        {
            BigDoubleFormat format = displayFormat switch
            {
                ResourceDisplayFormat.Standard => BigDoubleFormat.Standard,
                ResourceDisplayFormat.Engineering => BigDoubleFormat.Engineering,
                _ => BigDoubleFormat.Scientific
            };

            return value.ToShortString(3, format);
        }
    }
}
