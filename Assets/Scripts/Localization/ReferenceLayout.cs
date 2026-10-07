using UnityEngine;
namespace SecretFour.Localization
{
    [DisallowMultipleComponent]
    public class ReferenceLayout : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 last;
        private void OnEnable() { rect=(RectTransform)transform; Apply(); }
        private void LateUpdate()
        {
            if(rect.parent is RectTransform parent && last!=parent.rect.size) Apply();
        }
        private void Apply()
        {
            if (!(rect.parent is RectTransform parent)) return;
            last=parent.rect.size;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(1080,1920);
            float scale=Mathf.Min(last.x/1080,last.y/1920);
            rect.localScale=Vector3.one*Mathf.Max(.01f,scale);
        }
    }
}
