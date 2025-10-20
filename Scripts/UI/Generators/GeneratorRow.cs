using System.Collections.Generic;
using System.Text;
using GalacticExpansion.Core;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GalacticExpansion.UI.Generators
{
    /// <summary>
    /// Presents generator purchase controls with multi-buy buttons.
    /// </summary>
    public sealed class GeneratorRow : MonoBehaviour
    {
        [SerializeField] private Image iconImage = null!;
        [SerializeField] private TextMeshProUGUI nameLabel = null!;
        [SerializeField] private TextMeshProUGUI levelLabel = null!;
        [SerializeField] private TextMeshProUGUI productionLabel = null!;
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
        private EconomyService _economy = null!;
        private GeneratorDef _definition = null!;

        /// <summary>
        /// Initializes the row with generator definition and economy references.
        /// </summary>
        public void Initialize(GeneratorDef definition, EconomyService economy)
        {
            _definition = definition;
            _economy = economy;

            if (iconImage != null)
            {
                iconImage.sprite = definition.Icon;
            }

            if (nameLabel != null)
            {
                nameLabel.text = definition.DisplayName;
            }

            BindButton(buyX1Button, buyX1Label, PurchaseMode.X1, "x1");
            BindButton(buyX10Button, buyX10Label, PurchaseMode.X10, "x10");
            BindButton(buyX100Button, buyX100Label, PurchaseMode.X100, "x100");
            BindButton(buyMaxButton, buyMaxLabel, PurchaseMode.Max, "Max");

            Refresh();
        }

        /// <summary>
        /// Refreshes the UI elements to reflect current state.
        /// </summary>
        public void Refresh()
        {
            if (_economy == null || _definition == null)
            {
                return;
            }

            if (!_economy.TryGetGeneratorRuntime(_definition.Id, out EconomyService.GeneratorRuntime runtime) || runtime == null)
            {
                lockedGroup?.SetActive(true);
                unlockedGroup?.SetActive(false);
                if (requirementLabel != null)
                {
                    requirementLabel.text = BuildRequirementText();
                }

                foreach (ButtonBinding binding in _buttons)
                {
                    binding.Button.interactable = false;
                    binding.Label.text = FormatCost(binding.Caption, BigDouble.Zero, 0, BigDouble.Zero, BigDouble.Zero, ResourceDisplayFormat.Scientific);
                }

                if (levelLabel != null)
                {
                    levelLabel.text = "Level 0";
                }

                if (productionLabel != null)
                {
                    productionLabel.text = "Locked";
                }

                return;
            }

            bool unlocked = runtime.Unlocked;
            lockedGroup?.SetActive(!unlocked);
            unlockedGroup?.SetActive(unlocked);

            if (!unlocked)
            {
                if (requirementLabel != null)
                {
                    requirementLabel.text = BuildRequirementText();
                }

                if (levelLabel != null)
                {
                    levelLabel.text = "Level 0";
                }

                foreach (ButtonBinding binding in _buttons)
                {
                    binding.Button.interactable = false;
                    binding.Label.text = FormatCost(binding.Caption, BigDouble.Zero, 0, BigDouble.Zero, BigDouble.Zero, ResourceDisplayFormat.Scientific);
                }

                if (productionLabel != null)
                {
                    productionLabel.text = "Locked";
                }

                return;
            }

            int level = runtime.Level;
            if (levelLabel != null)
            {
                levelLabel.text = $"Level {level}";
            }

            ResourceDisplayFormat displayFormat = ResourceDisplayFormat.Scientific;
            if (_economy.TryGetResource(_definition.ProducesResourceId, out ResourceDef producedResource))
            {
                displayFormat = producedResource.DisplayFormat;
            }

            if (_economy.TryGetGeneratorSnapshot(_definition.Id, out EconomyService.GeneratorSnapshot snapshot))
            {
                if (productionLabel != null)
                {
                    string finalText = snapshot.Production.ToShortString(3, ConvertFormat(displayFormat));
                    StringBuilder builder = new();
                    builder.Append("Base ");
                    builder.Append(snapshot.BasePerSecond.ToString("F2"));
                    builder.Append(" × Exp ");
                    builder.Append(snapshot.ExponentialFactor.ToString("F2"));
                    builder.Append(" × Add ");
                    builder.Append(snapshot.AdditiveFactor.ToString("F2"));
                    builder.Append(" × Mult ");
                    builder.Append(snapshot.MultiplicativeFactor.ToString("F2"));
                    builder.Append(" × Ext ");
                    builder.Append(snapshot.ExternalMultiplier.ToString("F2"));
                    builder.Append(" = ");
                    builder.Append(finalText);
                    builder.Append("/s");
                    productionLabel.text = builder.ToString();
                }
            }

            BigDouble available = _economy.GetResourceAmount(_definition.RequiresResourceId);
            ResourceDisplayFormat costFormat = ResourceDisplayFormat.Scientific;
            if (_economy.TryGetResource(_definition.RequiresResourceId, out ResourceDef costResource))
            {
                costFormat = costResource.DisplayFormat;
            }

            foreach (ButtonBinding binding in _buttons)
            {
                int quantity = binding.Mode == PurchaseMode.Max
                    ? _economy.GetAffordableQuantity(_definition.Id, binding.Mode)
                    : GetModeQuantity(binding.Mode);

                BigDouble cost = quantity > 0 ? _economy.GetNextCost(_definition.Id, quantity) : BigDouble.Zero;
                bool canAfford = quantity > 0 && available >= cost;

                string caption = binding.Mode == PurchaseMode.Max && quantity > 0
                    ? $"Max ({quantity})"
                    : binding.Caption;

                BigDouble currentProduction = BigDouble.Zero;
                BigDouble projectedProduction = BigDouble.Zero;
                if (quantity > 0)
                {
                    _economy.TryGetProductionProjection(_definition.Id, quantity, out currentProduction, out projectedProduction);
                }

                binding.Label.text = FormatCost(caption, cost, quantity > 0 ? quantity : GetModeQuantity(binding.Mode), currentProduction, projectedProduction, costFormat, displayFormat);
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
                if (_economy.TryBuyGenerator(_definition.Id, mode))
                {
                    Refresh();
                }
            });

            _buttons.Add(new ButtonBinding(button, label, mode, caption));
        }

        private string BuildRequirementText()
        {
            List<string> requirements = new();
            GeneratorUnlockCondition condition = _definition.UnlockCondition;
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

        private static int GetModeQuantity(PurchaseMode mode) => mode switch
        {
            PurchaseMode.X10 => 10,
            PurchaseMode.X100 => 100,
            _ => 1
        };

        private static string FormatCost(string caption, BigDouble cost, int quantity, BigDouble current, BigDouble projected, ResourceDisplayFormat costFormat, ResourceDisplayFormat productionFormat = ResourceDisplayFormat.Scientific)
        {
            string costText = quantity <= 0 || cost.IsZero
                ? "--"
                : cost.ToShortString(3, ConvertFormat(costFormat));

            string deltaText = string.Empty;
            if (quantity > 0 && projected > current)
            {
                BigDouble gain = projected - current;
                string formattedGain = gain.ToShortString(3, ConvertFormat(productionFormat));
                deltaText = $" → +{formattedGain}/s";
            }

            return $"{caption}\n{costText}{deltaText}";
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
