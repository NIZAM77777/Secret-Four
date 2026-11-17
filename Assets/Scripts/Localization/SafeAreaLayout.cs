using UnityEngine;
namespace SecretFour.Localization
{
    /// <summary>
    /// Fits its RectTransform to the device safe area. When <see cref="reserveAdBanner"/> is on,
    /// the bottom edge is also raised above the bottom banner ad so no UI is hidden behind it.
    /// </summary>
    [DisallowMultipleComponent]
    public class SafeAreaLayout : MonoBehaviour
    {
        /// <summary>Height in screen pixels currently reserved for a bottom banner ad (0 = none).</summary>
        public static float BottomReservedPixels { get; private set; }

        public static void SetBottomReservedPixels(float pixels)
        {
            BottomReservedPixels = Mathf.Max(0f, pixels);
        }

        [Tooltip("Keep this panel's content above the bottom banner ad.")]
        [SerializeField] private bool reserveAdBanner;

        private RectTransform rect;
        private Rect last;
        private Vector2 lastSize;
        private float lastReserved = -1f;

        private void OnEnable() { rect = (RectTransform)transform; Apply(); }

        private void Update()
        {
            float reserved = reserveAdBanner ? BottomReservedPixels : 0f;
            if (last != Screen.safeArea ||
                lastSize != new Vector2(Screen.width, Screen.height) ||
                !Mathf.Approximately(lastReserved, reserved))
                Apply();
        }

        private void Apply()
        {
            if (Screen.width == 0 || Screen.height == 0) return;
            last = Screen.safeArea;
            lastSize = new Vector2(Screen.width, Screen.height);
            lastReserved = reserveAdBanner ? BottomReservedPixels : 0f;

            Vector2 min = last.position;
            Vector2 max = last.position + last.size;
            if (lastReserved > 0f)
                min.y = Mathf.Min(max.y - 1f, Mathf.Max(min.y, last.yMin + lastReserved));

            rect.anchorMin = min / lastSize;
            rect.anchorMax = max / lastSize;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
