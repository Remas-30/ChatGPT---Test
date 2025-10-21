using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GalacticExpansion.Localization;
using GalacticExpansion.UI.Nav;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GalacticExpansion.UI.Help
{
    /// <summary>
    /// Renders localized help content using a markdown-lite parser.
    /// </summary>
    public sealed class HelpPage : MonoBehaviour
    {
        [SerializeField] private ScrollRect scrollRect = null!;
        [SerializeField] private RectTransform contentRoot = null!;
        [SerializeField] private TextMeshProUGUI headerTemplate = null!;
        [SerializeField] private TextMeshProUGUI paragraphTemplate = null!;
        [SerializeField] private TextMeshProUGUI listItemTemplate = null!;
        [SerializeField] private string localizationKey = "HELP_CONTENT_EN";
        [SerializeField] private UIRouter router = null!;

        private readonly Dictionary<string, RectTransform> _anchors = new();
        private void Start()
        {
            if (headerTemplate != null)
            {
                headerTemplate.gameObject.SetActive(false);
            }

            if (paragraphTemplate != null)
            {
                paragraphTemplate.gameObject.SetActive(false);
            }

            if (listItemTemplate != null)
            {
                listItemTemplate.gameObject.SetActive(false);
            }

            if (LocalizationProvider.TryGet(localizationKey, out string content))
            {
                RenderContent(content);
            }
        }

        /// <summary>
        /// Scrolls to a section header with the supplied label.
        /// </summary>
        public void ScrollToSection(string header)
        {
            string normalizedHeader = header.ToUpperInvariant();
            if (!_anchors.TryGetValue(normalizedHeader, out RectTransform target) || scrollRect == null || contentRoot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            RectTransform viewport = scrollRect.viewport;
            if (viewport == null)
            {
                return;
            }

            float contentHeight = contentRoot.rect.height;
            float viewportHeight = viewport.rect.height;
            float targetY = Mathf.Abs(target.anchoredPosition.y);
            float normalized = 1f;
            if (contentHeight > viewportHeight)
            {
                normalized = 1f - Mathf.Clamp01(targetY / (contentHeight - viewportHeight));
            }

            scrollRect.verticalNormalizedPosition = normalized;
        }

        private void RenderContent(string content)
        {
            if (contentRoot == null)
            {
                return;
            }

            List<string> paragraphBuffer = new();
            using StringReader reader = new(content);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushParagraph(paragraphBuffer);
                    continue;
                }

                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    FlushParagraph(paragraphBuffer);
                    CreateHeader(line.Substring(3), 2);
                }
                else if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    FlushParagraph(paragraphBuffer);
                    CreateHeader(line.Substring(2), 1);
                }
                else if (line.StartsWith("- ", StringComparison.Ordinal))
                {
                    FlushParagraph(paragraphBuffer);
                    CreateListItem(line.Substring(2));
                }
                else
                {
                    paragraphBuffer.Add(line);
                }
            }

            FlushParagraph(paragraphBuffer);
        }

        private void FlushParagraph(List<string> buffer)
        {
            if (buffer.Count == 0)
            {
                return;
            }

            string text = string.Join("\n", buffer);
            CreateParagraph(text);
            buffer.Clear();
        }

        private void CreateHeader(string text, int level)
        {
            if (headerTemplate == null || contentRoot == null)
            {
                return;
            }

            TextMeshProUGUI instance = Instantiate(headerTemplate, contentRoot);
            instance.gameObject.SetActive(true);
            instance.text = ApplyInlineFormatting(text);
            AttachLinkHandler(instance);
            instance.fontSize = level == 1 ? headerTemplate.fontSize : headerTemplate.fontSize * 0.85f;

            string anchorKey = text.Trim();
            string normalized = anchorKey.ToUpperInvariant();
            if (!_anchors.ContainsKey(normalized))
            {
                _anchors[normalized] = instance.rectTransform;
            }
        }

        private void CreateParagraph(string text)
        {
            if (paragraphTemplate == null || contentRoot == null)
            {
                return;
            }

            TextMeshProUGUI instance = Instantiate(paragraphTemplate, contentRoot);
            instance.gameObject.SetActive(true);
            instance.text = ApplyInlineFormatting(text);
            AttachLinkHandler(instance);
        }

        private void CreateListItem(string text)
        {
            if (listItemTemplate == null || contentRoot == null)
            {
                return;
            }

            TextMeshProUGUI instance = Instantiate(listItemTemplate, contentRoot);
            instance.gameObject.SetActive(true);
            instance.text = $"• {ApplyInlineFormatting(text)}";
            AttachLinkHandler(instance);
        }

        private static string ApplyInlineFormatting(string text)
        {
            StringBuilder builder = new(text.Length + 16);
            bool boldOpen = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (i < text.Length - 1 && text[i] == '*' && text[i + 1] == '*')
                {
                    builder.Append(boldOpen ? "</b>" : "<b>");
                    boldOpen = !boldOpen;
                    i++;
                    continue;
                }

                if (text[i] == '[')
                {
                    int closeBracket = text.IndexOf(']', i);
                    if (closeBracket > i)
                    {
                        int openParen = closeBracket + 1 < text.Length && text[closeBracket + 1] == '(' ? closeBracket + 1 : -1;
                        if (openParen > 0)
                        {
                            int closeParen = text.IndexOf(')', openParen);
                            if (closeParen > openParen)
                            {
                                string label = text.Substring(i + 1, closeBracket - i - 1);
                                string link = text.Substring(openParen + 1, closeParen - openParen - 1);
                                builder.Append('<').Append("link=\"").Append(link).Append("\">");
                                builder.Append(label);
                                builder.Append("</link>");
                                i = closeParen;
                                continue;
                            }
                        }
                    }
                }

                builder.Append(text[i]);
            }

            if (boldOpen)
            {
                builder.Append("</b>");
            }

            return builder.ToString();
        }

        private void AttachLinkHandler(TextMeshProUGUI textComponent)
        {
            if (textComponent == null || router == null)
            {
                return;
            }

            HelpLinkHandler handler = textComponent.GetComponent<HelpLinkHandler>();
            if (handler == null)
            {
                handler = textComponent.gameObject.AddComponent<HelpLinkHandler>();
            }

            handler.Initialize(router);
        }
    }
}
