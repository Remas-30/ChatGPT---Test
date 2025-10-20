using GalacticExpansion.UI.Nav;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GalacticExpansion.UI.Help
{
    /// <summary>
    /// Handles click events on TMP link markup within help text blocks.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class HelpLinkHandler : MonoBehaviour, IPointerClickHandler
    {
        private TextMeshProUGUI _text = null!;
        private UIRouter _router = null!;

        /// <summary>
        /// Initializes the link handler with a router reference.
        /// </summary>
        public void Initialize(UIRouter router)
        {
            _router = router;
        }

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_text == null || _router == null)
            {
                return;
            }

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(_text, eventData.position, eventData.pressEventCamera);
            if (linkIndex == -1)
            {
                return;
            }

            TMP_LinkInfo linkInfo = _text.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();
            _router.Navigate(linkId);
        }
    }
}
