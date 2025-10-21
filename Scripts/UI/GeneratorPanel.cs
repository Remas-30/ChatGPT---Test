using GalacticExpansion.Core;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using UnityEngine;
using UnityEngine.UI;

namespace GalacticExpansion.UI
{
    /// <summary>
    /// UI component for purchasing generator levels.
    /// </summary>
    public sealed class GeneratorPanel : MonoBehaviour
    {
        [SerializeField] private GeneratorDef generator = null!;
        [SerializeField] private Text titleLabel = null!;
        [SerializeField] private Text levelLabel = null!;
        [SerializeField] private Text costLabel = null!;
        [SerializeField] private Button buyOneButton = null!;
        [SerializeField] private Button buyTenButton = null!;
        [SerializeField] private Button buyHundredButton = null!;

        private EconomyService _economy = null!;

        private void Awake()
        {
            buyOneButton.onClick.AddListener(() => Purchase(1));
            buyTenButton.onClick.AddListener(() => Purchase(10));
            buyHundredButton.onClick.AddListener(() => Purchase(100));
        }

        private void Start()
        {
            _economy = ServiceLocator.Get<EconomyService>();
            titleLabel.text = generator.DisplayName;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_economy == null)
            {
                return;
            }

            if (_economy.TryGetGeneratorRuntime(generator.Id, out EconomyService.GeneratorRuntime runtime))
            {
                levelLabel.text = $"Level {runtime.Level}";
                BigDouble cost = _economy.CalculateGeneratorCost(generator, runtime.Level, 1);
                costLabel.text = $"Cost: {cost.ToString()} {generator.CostResource.DisplayName}";
            }
        }

        private void Purchase(int quantity)
        {
            _economy.TryPurchaseGenerator(generator, quantity);
        }
    }
}
