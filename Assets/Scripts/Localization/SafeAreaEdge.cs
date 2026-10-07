using UnityEngine;
namespace SecretFour.Localization
{
    public class SafeAreaEdge : MonoBehaviour
    {
        public bool top;
        private RectTransform rect;
        private Vector2 original;
        private Canvas canvas;
        private void Awake(){rect=(RectTransform)transform;original=rect.anchoredPosition;canvas=GetComponentInParent<Canvas>();}
        private void LateUpdate()
        {
            float inset=top ? Screen.height-Screen.safeArea.yMax : Screen.safeArea.yMin;
            rect.anchoredPosition=original+new Vector2(0,(top?-inset:inset)/canvas.scaleFactor);
        }
    }
}
