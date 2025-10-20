using System.Collections.Generic;
using GalacticExpansion.Core;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GalacticExpansion.UI.Upgrades
{
    /// <summary>
    /// Displays a single upgrade entry with multi-buy controls.
    /// </summary>
    public sealed class UpgradeRow : MonoBehaviour
    {
        [SerializeField] private Image iconImage = null!;
        [SerializeField] private TextMeshProUGUI nameLabel = null!;
        [SerializeField] private TextMeshProUGUI descriptionLabel = null!;
        [SerializeField] private TextMeshProUGUI levelLabel = null!;
        [SerializeField] private TextMeshProUGUI effectLabel = null!;
        [SerializeField] private TextMeshProUGUI requirementLabel = null!;
        [SerializeField] private GameObject lockedGroup = null!;
        [SerializeField] private GameObject unlockedGroup = null!;
        [SerializeField] private Button buyX1Button = null!;
        [SerializeField] private TextMeshProUGUI buyX1Label = null!;
        [SerializeField] private Button buyX10Button = null!;
        [SerializeField] private TextMeshProUGUI buyX10Label = null!;
        [SerializeField] private Button buyX100Button = null!;
        [SerializeField] private TextMeshProUGUI buyX100Label = null!;
        [SerializeField] private Button buyMaxButton = null!;
        [SerializeField] private TextMeshProUGUI buyMaxLabel = null!;

        private readonly List<ButtonBinding> _buttons = new();
        private UpgradeDef _definition = null!;
        private UpgradeService _upgrades = null!;
        private EconomyService _economy = null!;
        private MetaCurrencyService _meta = null!;

        /// <summary>
        /// Initializes the row with upgrade and service references.
        /// </summary>
        public void Initialize(UpgradeDef def, UpgradeService upgradeService, EconomyService economy, MetaCurrencyService meta)
        {
            _definition = def;
            _upgrades = upgradeService;
            _economy = economy;
            _meta = meta;

            if (iconImage != null)
            {
                iconImage.sprite = def.Icon;
            }

            if (nameLabel != null)
            {
                nameLabel.text = def.DisplayName;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = def.Description;
            }

            effectLabel?.SetText(BuildEffectText());

            BindButton(buyX1Button, buyX1Label, PurchaseMode.X1, "x1");
            BindButton(buyX10Button, buyX10Label, PurchaseMode.X10, "x10");
            BindButton(buyX100Button, buyX100Label, PurchaseMode.X100, "x100");
            BindButton(buyMaxButton, buyMaxLabel, PurchaseMode.Max, "Max");

            Refresh();
        }

        /// <summary>
        /// Refreshes the row contents to reflect the latest state.
        /// </summary>
        public void Refresh()
        {
            if (_definition == null || _upgrades == null)
            {
                return;
            }

            if (!_upgrades.TryGetRuntime(_definition.Id, out UpgradeService.UpgradeRuntime runtime) || runtime == null)
            {
                return;
            }

            bool unlocked = runtime.Unlocked;
            lockedGroup?.SetActive(!unlocked);
            unlockedGroup?.SetActive(unlocked);

            if (levelLabel != null)
            {
                if (_definition.MaxLevel >= 0)
                {
                    levelLabel.text = $"Level {runtime.Level}/{_definition.MaxLevel}";
                }
                else
                {
                    levelLabel.text = $"Level {runtime.Level}";
                }
            }

            if (!unlocked)
            {
                requirementLabel?.SetText(BuildRequirementText());
                foreach (ButtonBinding binding in _buttons)
                {
                    binding.Button.interactable = false;
                    binding.Label.text = FormatCost(binding.Caption, BigDouble.Zero, 0);
                }

                return;
            }

            requirementLabel?.SetText(string.Empty);

            BigDouble available = GetAvailable(_definition.CostResourceId);
            ResourceDisplayFormat costFormat = ResourceDisplayFormat.Scientific;
            if (_economy.TryGetResource(_definition.CostResourceId, out ResourceDef costResource))
            {
                costFormat = costResource.DisplayFormat;
            }

            foreach (ButtonBinding binding in _buttons)
            {
                int quantity = binding.Mode == PurchaseMode.Max
                    ? _upgrades.GetMaxAffordableQty(_definition.Id)
                    : GetModeQuantity(binding.Mode);

                if (_definition.MaxLevel >= 0)
                {
                    int remaining = _definition.MaxLevel - runtime.Level;
                    quantity = Mathf.Clamp(quantity, 0, remaining);
                }

                BigDouble cost = quantity > 0 ? _upgrades.GetNextCost(_definition.Id, quantity) : BigDouble.Zero;
                bool canAfford = quantity > 0 && available >= cost;

                string caption = binding.Mode == PurchaseMode.Max && quantity > 0
                    ? $"Max ({quantity})"
                    : binding.Caption;

                binding.Label.text = FormatCost(caption, cost, quantity, costFormat);
                binding.Button.interactable = canAfford && quantity > 0;
            }
        }

        private void BindButton(Button button, TextMeshProUGUI label, PurchaseMode mode, string caption)
        {
            if (button == null || label == null)
            {
                return;
            }

            button.onClick.AddListener(() =>
            {
                if (_upgrades.TryBuy(_definition.Id, mode))
                {
                    Refresh();
                }
            });

            _buttons.Add(new ButtonBinding(button, label, mode, caption));
        }

        private string BuildRequirementText()
        {
            List<string> requirements = new();
            UpgradeUnlockCondition condition = _definition.UnlockCondition;
            if (condition.HasResourceGate && _economy.TryGetResource(condition.RequiredResourceId, out ResourceDef resource))
            {
                requirements.Add($"Requires {resource.DisplayName} ≥ {condition.RequiredResourceAmount:F0}");
            }

            if (condition.HasMapGate)
            {
                requirements.Add($"Unlock map node {condition.RequiredMapNodeId}");
            }

            return requirements.Count == 0 ? "Unlock via progression" : string.Join("\n", requirements);
        }

        private string BuildEffectText()
        {
            string target = _definition.Tags.Count > 0 ? string.Join(", ", _definition.Tags) : "Targets";
            return _definition.EffectType switch
            {
                UpgradeEffectType.Additive => $"+{_definition.EffectValue * 100f:F1}% additive to {target}",
                UpgradeEffectType.Multiplicative => $"×{1d + _definition.EffectValue:F2} per level ({target})",
                UpgradeEffectType.Exponential => $"×{_definition.EffectValue:F2}^level base ({target})",
                _ => string.Empty
            };
        }

        private BigDouble GetAvailable(string resourceId)
        {
            BigDouble amount = _economy.GetResourceAmount(resourceId);
            BigDouble metaAmount = _meta.GetCurrency(resourceId);
            return BigDouble.Max(amount, metaAmount);
        }

        private static int GetModeQuantity(PurchaseMode mode) => mode switch
        {
            PurchaseMode.X10 => 10,
            PurchaseMode.X100 => 100,
            _ => 1
        };

        private static string FormatCost(string caption, BigDouble cost, int quantity, ResourceDisplayFormat format = ResourceDisplayFormat.Scientific)
        {
            string costText = quantity <= 0 || cost.IsZero
                ? "--"
                : cost.ToShortString(3, ConvertFormat(format));
            return $"{caption}\n{costText}";
        }

        private static BigDoubleFormat ConvertFormat(ResourceDisplayFormat displayFormat) => displayFormat switch
        {
            ResourceDisplayFormat.Standard => BigDoubleFormat.Standard,
            ResourceDisplayFormat.Engineering => BigDoubleFormat.Engineering,
            _ => BigDoubleFormat.Scientific
        };

        private readonly struct ButtonBinding
        {
            public ButtonBinding(Button button, TextMeshProUGUI label, PurchaseMode mode, string caption)
            {
                Button = button;
                Label = label;
                Mode = mode;
                Caption = caption;
            }

            public Button Button { get; }
            public TextMeshProUGUI Label { get; }
            public PurchaseMode Mode { get; }
            public string Caption { get; }
        }
    }
}
