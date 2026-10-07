using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SecretFour.Localization;
namespace SecretFour.Presentation
{
    public class ResultPresentation : MonoBehaviour
    {
        public Image winnerImage;
        public RectTransform background,title,playAgain,mainMenu;
        public GameObject drawEmblem, victoryCrown;
        public TMP_Text caption;
        public Vector2 victorySize=new Vector2(790,1030),drawSize=new Vector2(790,740);
        public Vector2 victoryTitle=new Vector2(0,310),drawTitle=new Vector2(0,85);
        public Vector2 victoryPlayAgain=new Vector2(0,-305),drawPlayAgain=new Vector2(0,-120);
        public Vector2 victoryMainMenu=new Vector2(0,-435),drawMainMenu=new Vector2(0,-260);
        public Vector2 victoryCaption=new Vector2(0,-210),drawCaption=new Vector2(0,-20);
        private bool? wasDraw;
        void OnEnable(){wasDraw=null;}
        void LateUpdate()
        {
            bool draw=winnerImage!=null&&!winnerImage.gameObject.activeSelf;
            if(caption)caption.text=GameLocalization.Text(draw?"ui.drawHint":"ui.victoryHint");
            if(wasDraw==draw)return;wasDraw=draw;
            background.sizeDelta=draw?drawSize:victorySize;
            title.anchoredPosition=draw?drawTitle:victoryTitle;
            playAgain.anchoredPosition=draw?drawPlayAgain:victoryPlayAgain;
            mainMenu.anchoredPosition=draw?drawMainMenu:victoryMainMenu;
            if(caption)caption.rectTransform.anchoredPosition=draw?drawCaption:victoryCaption;
            if(drawEmblem)drawEmblem.SetActive(draw);
            if(victoryCrown)victoryCrown.SetActive(!draw);
        }
    }
}

