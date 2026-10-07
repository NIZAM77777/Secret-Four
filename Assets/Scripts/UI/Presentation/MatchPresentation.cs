using SecretFour.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace SecretFour.Presentation
{
    // Observes game state for decoration only. Never issues a game action.
    public class MatchPresentation : MonoBehaviour
    {
        public TurnManager turnManager;
        public TMP_Text guide, directionGuide;
        public Image[] phaseUnderlines;
        public TMP_Text[] phases;
        public Image xUnderline,yUnderline;
        public Color active=new Color(1,.83f,.48f),inactive=new Color(.48f,.52f,.56f);
        private void LateUpdate()
        {
            if(turnManager==null)return;
            var state=turnManager.CurrentState;
            bool place=state==GameState.WaitingForPlayerXPlacement||state==GameState.WaitingForPlayerYPlacement;
            bool choose=state==GameState.WaitingForPlayerXDirection||state==GameState.WaitingForPlayerYDirection;
            int phase=place?0:choose?1:2;
            for(int i=0;i<phases.Length;i++){phases[i].color=i==phase?active:inactive;phaseUnderlines[i].enabled=i==phase;}
            bool x=state==GameState.WaitingForPlayerXPlacement||state==GameState.WaitingForPlayerXDirection;
            bool y=state==GameState.WaitingForPlayerYPlacement||state==GameState.WaitingForPlayerYDirection;
            if(xUnderline)xUnderline.enabled=x;if(yUnderline)yUnderline.enabled=y;
            if(guide)guide.text=GameLocalization.Text(place?"ui.placeHint":choose?"ui.chooseHint":"ui.revealHint");
            if(directionGuide)directionGuide.text=GameLocalization.Text(place?"ui.placeToChoose":choose?"ui.chooseHint":"ui.revealHint");
        }
    }
}
