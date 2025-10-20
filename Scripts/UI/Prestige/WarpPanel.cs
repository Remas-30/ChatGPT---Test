using GalacticExpansion.Core;
using GalacticExpansion.Data;
using GalacticExpansion.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GalacticExpansion.UI.Prestige
{
    /// <summary>
    /// Displays Warp prestige information and triggers the reset flow.
    /// </summary>
    public sealed class WarpPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI warpCoreLabel = null!;
        [SerializeField] private TextMeshProUGUI rewardLabel = null!;
        [SerializeField] private TextMeshProUGUI requirementLabel = null!;
        [SerializeField] private TextMeshProUGUI statusLabel = null!;
        [SerializeField] private Button prestigeButton = null!;
        [SerializeField] private Button confirmButton = null!;
        [SerializeField] private Button cancelButton = null!;
        [SerializeField] private GameObject confirmationGroup = null!;

        private PrestigeService _prestigeService = null!;
        private MetaCurrencyService _metaService = null!;
        private bool _awaitingConfirmation;

        private void Awake()
        {
            _prestigeService = ServiceLocator.Get<PrestigeService>();
            _metaService = ServiceLocator.Get<MetaCurrencyService>();

            if (confirmationGroup != null)
            {
                confirmationGroup.SetActive(false);
            }

            if (prestigeButton != null)
            {
                prestigeButton.onClick.AddListener(BeginPrestigeFlow);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmPrestige);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(CancelPrestige);
            }

            _prestigeService.WarpEligibilityChanged += OnEligibilityChanged;
            _prestigeService.PrestigePerformed += OnPrestigePerformed;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_prestigeService != null)
            {
                _prestigeService.WarpEligibilityChanged -= OnEligibilityChanged;
                _prestigeService.PrestigePerformed -= OnPrestigePerformed;
            }
        }

        private void Update()
        {
            Refresh();
        }

        private void BeginPrestigeFlow()
        {
            if (!_prestigeService.IsWarpEligible)
            {
                statusLabel?.SetText("Warp prestige unavailable.");
                return;
            }

            _awaitingConfirmation = true;
            if (confirmationGroup != null)
            {
                confirmationGroup.SetActive(true);
            }

            statusLabel?.SetText("Confirm Warp reset?");
        }

        private void ConfirmPrestige()
        {
            if (!_awaitingConfirmation)
            {
                return;
            }

            if (_prestigeService.TryPrestigeWarp())
            {
                statusLabel?.SetText("Warp complete! Cores secured.");
            }
            else
            {
                statusLabel?.SetText("Warp failed. Check requirements.");
            }

            _awaitingConfirmation = false;
            if (confirmationGroup != null)
            {
                confirmationGroup.SetActive(false);
            }
        }

        private void CancelPrestige()
        {
            _awaitingConfirmation = false;
            if (confirmationGroup != null)
            {
                confirmationGroup.SetActive(false);
            }

            statusLabel?.SetText(string.Empty);
        }

        private void OnEligibilityChanged(bool eligible)
        {
            if (prestigeButton != null)
            {
                prestigeButton.interactable = eligible;
            }

            if (!eligible && confirmationGroup != null)
            {
                confirmationGroup.SetActive(false);
            }
        }

        private void OnPrestigePerformed(PrestigeDef _, BigDouble reward)
        {
            statusLabel?.SetText($"Warp granted {reward.ToShortString(3, BigDoubleFormat.Scientific)} cores!");
            Refresh();
        }

        private void Refresh()
        {
            if (_prestigeService == null || _metaService == null)
            {
                return;
            }

            BigDouble warpCores = _metaService.GetCurrency("WarpCores");
            warpCoreLabel?.SetText(warpCores.ToShortString(3, BigDoubleFormat.Scientific));

            double requirement;
            BigDouble progress = _prestigeService.GetWarpRequirementProgress(out requirement);
            BigDouble requiredValue = BigDouble.FromDouble(requirement);
            requirementLabel?.SetText($"Lifetime Credits: {progress.ToShortString(3, BigDoubleFormat.Scientific)} / {requiredValue.ToShortString(3, BigDoubleFormat.Scientific)}");

            BigDouble reward = _prestigeService.GetProjectedWarpReward();
            rewardLabel?.SetText(reward.ToShortString(3, BigDoubleFormat.Scientific));

            if (prestigeButton != null)
            {
                prestigeButton.interactable = _prestigeService.IsWarpEligible;
            }
        }
    }
}
