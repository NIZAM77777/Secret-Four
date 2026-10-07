using UnityEngine;
namespace SecretFour.Localization
{
    [DisallowMultipleComponent]
    public class SafeAreaLayout : MonoBehaviour
    {
        private RectTransform rect;
        private Rect last;
        private Vector2 lastSize;
        private void OnEnable() { rect = (RectTransform)transform; Apply(); }
        private void Update()
        {
            if (last != Screen.safeArea || lastSize != new Vector2(Screen.width, Screen.height)) Apply();
        }
        private void Apply()
        {
            if (Screen.width == 0 || Screen.height == 0) return;
            last = Screen.safeArea;
            lastSize = new Vector2(Screen.width, Screen.height);
            rect.anchorMin = last.position / lastSize;
            rect.anchorMax = (last.position + last.size) / lastSize;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
