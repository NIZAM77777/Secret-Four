using RTLTMPro;
using TMPro;
using UnityEngine;

namespace SecretFour.Localization
{
    [DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        public string key;
        public bool useTitleFont;
        public bool mirrorAlignment = true;
        private TMP_Text label;
        private TextAlignmentOptions originalAlignment;
        private string previousText;
        private bool initialized;
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (initialized) return;
            label = GetComponent<TMP_Text>();
            originalAlignment = label.alignment;
            initialized = true;
        }
        private void OnEnable() { Initialize(); GameLocalization.Changed += Refresh; Refresh(); }
        private void OnDisable() => GameLocalization.Changed -= Refresh;
        public void Refresh()
        {
            Initialize();
            if (!string.IsNullOrEmpty(key)) label.text = GameLocalization.Text(key);
            ApplyTypeface();
        }
        private void LateUpdate()
        {
            string current = label is RTLTextMeshPro rtl ? rtl.OriginalText : label.text;
            if (current != previousText) ApplyTypeface();
        }
        private void ApplyTypeface()
        {
            string current = label is RTLTextMeshPro rtlLabel ? rtlLabel.OriginalText : label.text;
            bool arabic = GameLocalization.ContainsArabic(current);
            GameTextCatalog catalog = GameLocalization.Catalog;
            if (catalog != null)
            {
                var font = arabic ? catalog.arabicFont : useTitleFont ? catalog.titleFont : catalog.latinFont;
                if (font != null && label.font != font) label.font = font;
            }
            if (label is RTLTextMeshPro rtl)
            {
                rtl.Farsi = false;
                rtl.PreserveNumbers = true;
                rtl.ForceFix = arabic;
                rtl.FixTags = true;
            }
            label.alignment = originalAlignment;
            if (mirrorAlignment && GameLocalization.IsRTL)
            {
                int alignment = (int)originalAlignment;
                int horizontal = alignment & 0xff;
                if (horizontal == 1) label.alignment = (TextAlignmentOptions)((alignment & ~0xff) | 4);
                else if (horizontal == 4) label.alignment = (TextAlignmentOptions)((alignment & ~0xff) | 1);
            }
            previousText = current;
        }
    }
}
