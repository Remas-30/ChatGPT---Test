using GalacticExpansion.Core;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using UnityEngine;
using UnityEngine.UI;

namespace GalacticExpansion.UI
{
    /// <summary>
    /// Displays resource amounts and production per second on the HUD.
    /// </summary>
    public sealed class ResourceBarView : MonoBehaviour
    {
        [SerializeField] private ResourceDef resource = null!;
        [SerializeField] private Text amountLabel = null!;
        [SerializeField] private Text productionLabel = null!;

        private EconomyService _economy = null!;

        private void Start()
        {
            _economy = ServiceLocator.Get<EconomyService>();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_economy == null || resource == null)
            {
                return;
            }

            BigDouble amount = _economy.GetResourceAmount(resource);
            BigDouble perSecond = _economy.GetProductionPerSecond(resource);
            amountLabel.text = amount.ToString();
            productionLabel.text = $"{perSecond.ToString()}/s";
        }
    }
}
