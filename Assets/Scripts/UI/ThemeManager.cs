using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace YallaCatch.UI
{
    /// <summary>
    /// Centralized Theme Manager to enforce the "Premium Rich" design system
    /// Attach to a global manager or Canvas root.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }

        [Header("Brand Palette (Premium Rich)")]
        public Color primaryColor = new Color(1f, 0.75f, 0f);     // Amber
        public Color secondaryColor = new Color(0f, 0.8f, 0.8f);   // Teal
        public Color accentColor = new Color(0.3f, 0f, 0.5f);      // Indigo/Purple
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.2f); // Dark Indigo
        public Color textColorLight = Color.white;
        public Color textColorDark = new Color(0.1f, 0.1f, 0.1f);
        
        [Header("Glassmorphism Settings")]
        public float panelAlpha = 0.85f;
        public Color glassColor = new Color(1f, 1f, 1f, 0.1f);
        public Color glassBorderColor = new Color(1f, 1f, 1f, 0.3f);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// Applies the premium style to a background panel (dark with slight transparency).
        /// </summary>
        public void ApplyGlassmorphism(Image panelImage)
        {
            if (panelImage == null) return;
            panelImage.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, panelAlpha);
            // In a real scenario, you'd apply a Blur material here if available.
        }

        /// <summary>
        /// Styles a button with the primary Amber brand color.
        /// </summary>
        public void StylePrimaryButton(Button btn, TextMeshProUGUI btnText = null)
        {
            if (btn == null) return;
            ColorBlock cb = btn.colors;
            cb.normalColor = primaryColor;
            cb.highlightedColor = Color.Lerp(primaryColor, Color.white, 0.2f);
            cb.pressedColor = Color.Lerp(primaryColor, Color.black, 0.2f);
            btn.colors = cb;

            if (btnText != null) btnText.color = textColorDark;
        }

        /// <summary>
        /// Styles a button with the secondary Teal brand color.
        /// </summary>
        public void StyleSecondaryButton(Button btn, TextMeshProUGUI btnText = null)
        {
            if (btn == null) return;
            ColorBlock cb = btn.colors;
            cb.normalColor = secondaryColor;
            cb.highlightedColor = Color.Lerp(secondaryColor, Color.white, 0.2f);
            cb.pressedColor = Color.Lerp(secondaryColor, Color.black, 0.2f);
            btn.colors = cb;

            if (btnText != null) btnText.color = textColorLight;
        }

        /// <summary>
        /// Gets the color associated with a prize rarity.
        /// </summary>
        public Color GetRarityColor(string rarity)
        {
            return rarity?.ToLower() switch
            {
                "common" => Color.gray,
                "uncommon" => Color.green,
                "rare" => secondaryColor, // Teal
                "epic" => accentColor,    // Purple
                "legendary" => primaryColor, // Amber/Gold
                _ => Color.white
            };
        }
    }
}
