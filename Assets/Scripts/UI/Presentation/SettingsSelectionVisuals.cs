using SecretFour.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SecretFour.Presentation
{
    [AddComponentMenu("Secret Four/UI/Settings Selection Visuals")]
    public class SettingsSelectionVisuals : MonoBehaviour
    {
        public Button[] languages;
        public GameObject[] checks;
        public Sprite selectedSprite, normalSprite;
        public RectTransform muteKnob;
        public Image muteTrack;
        public Color selectedText = new Color(.02f,.055f,.075f), normalText = new Color(1,.97f,.9f);
        private void LateUpdate()
        {
            for(int i=0;i<languages.Length;i++)
            {
                bool selected=i==(int)GameLocalization.Language;
                if(languages[i].image!=null) languages[i].image.sprite=selected?selectedSprite:normalSprite;
                foreach(var text in languages[i].GetComponentsInChildren<TMP_Text>())text.color=selected?selectedText:normalText;
                if(i<checks.Length && checks[i]!=null) checks[i].SetActive(selected);
            }
            if(muteKnob!=null)muteKnob.anchoredPosition=new Vector2(AudioPreferences.Muted?28:-28,0);
            if(muteTrack!=null)muteTrack.color=AudioPreferences.Muted?new Color(.9f,.69f,.32f):new Color(.25f,.33f,.39f);
        }
    }
}
