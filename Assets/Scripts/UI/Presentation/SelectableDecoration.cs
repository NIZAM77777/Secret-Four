using UnityEngine;
using UnityEngine.UI;
namespace SecretFour.Presentation
{
    // Keeps only decorative children in sync with an existing Selectable's enabled state.
    public class SelectableDecoration : MonoBehaviour
    {
        public Selectable control;
        public Graphic[] decorations;
        public Color enabledColor=new Color(1,.81f,.43f),disabledColor=new Color(.42f,.47f,.52f);
        private void LateUpdate(){if(control==null)return;foreach(var graphic in decorations)if(graphic)graphic.color=control.IsInteractable()?enabledColor:disabledColor;}
    }
}
