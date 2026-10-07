using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace SecretFour.Localization
{
    [DisallowMultipleComponent]
    public class ButtonFeedback : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            var selectable = GetComponent<Selectable>();
            if (selectable != null && selectable.IsInteractable()) GameAudio.Play("click");
        }
    }
}
