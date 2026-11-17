using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace SecretFour.Localization
{
    /// <summary>
    /// Plays a UI sound when its button is tapped. Buttons whose name means "go back"
    /// (Back, Close, Cancel, Main Menu, Exit) use the softer "back" sound automatically.
    /// Set <see cref="cue"/> to force a specific sound, or "none" for silence.
    /// </summary>
    [DisallowMultipleComponent]
    public class ButtonFeedback : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Optional sound override: click, back, select, none… Leave empty for automatic.")]
        public string cue;

        private static readonly string[] BackWords = { "back", "close", "cancel", "mainmenu", "main menu", "exit", "resume" };

        public void OnPointerClick(PointerEventData eventData)
        {
            var selectable = GetComponent<Selectable>();
            if (selectable == null || !selectable.IsInteractable()) return;

            string sound = string.IsNullOrWhiteSpace(cue) ? AutomaticCue() : cue.Trim().ToLowerInvariant();
            if (sound != "none") GameAudio.Play(sound);
        }

        private string AutomaticCue()
        {
            string n = gameObject.name.ToLowerInvariant();
            foreach (string word in BackWords)
                if (n.Contains(word)) return "back";
            return "click";
        }
    }
}
