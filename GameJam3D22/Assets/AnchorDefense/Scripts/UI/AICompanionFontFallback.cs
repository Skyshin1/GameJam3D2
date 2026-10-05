using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AnchorDefense
{
    // Preserve the artist's primary typeface. Only missing glyphs use the fallback.
    public sealed class AICompanionFontFallback : MonoBehaviour
    {
        private readonly List<TMP_FontAsset> clones = new List<TMP_FontAsset>();

        public void Initialize(TMP_FontAsset suppliedFallback)
        {
            TMP_FontAsset fallback = suppliedFallback;
            if (fallback == null) return;
            var replacements = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == null) continue;
                if (!replacements.TryGetValue(text.font, out TMP_FontAsset replacement))
                {
                    replacement = Instantiate(text.font);
                    replacement.name = text.font.name + " (Companion Instance)";
                    replacement.fallbackFontAssetTable = text.font.fallbackFontAssetTable != null
                        ? new List<TMP_FontAsset>(text.font.fallbackFontAssetTable) : new List<TMP_FontAsset>();
                    if (!replacement.fallbackFontAssetTable.Contains(fallback)) replacement.fallbackFontAssetTable.Add(fallback);
                    replacements.Add(text.font, replacement); clones.Add(replacement);
                }
                text.font = replacement;
            }
            foreach (TMP_InputField field in GetComponentsInChildren<TMP_InputField>(true))
                if (field.textComponent != null) field.fontAsset = field.textComponent.font;
        }
        private void OnDestroy()
        {
            foreach (TMP_FontAsset clone in clones) if (clone != null) Destroy(clone);
        }
    }
}
