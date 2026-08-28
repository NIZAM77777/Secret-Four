using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private TMP_Text resultTitle;

    [SerializeField]
    private TMP_Text resultSubtitle;

    [SerializeField]
    private MatchManager matchManager;

    [SerializeField]
    private GamePlayerInfo gamePlayerInfo;


    private void Start()
    {
        Hide();
    }


    public void ShowWinner(
        PlayerType winner)
    {
        panel.SetActive(true);

        string winnerName =
            gamePlayerInfo.GetPlayerName(
                winner
            );

        resultTitle.text =
            $"{winnerName.ToUpper()} WINS!";

        if (gamePlayerInfo.IsBot(winner))
        {
            resultSubtitle.text =
                "Better luck next time!";
        }
        else
        {
            resultSubtitle.text =
                "Four in a row!";
        }
    }


    public void ShowDraw()
    {
        panel.SetActive(true);

        resultTitle.text = "DRAW!";

        resultSubtitle.text =
            "No winner this time.";
    }


    public void Hide()
    {
        panel.SetActive(false);
    }


    public void OnPlayAgainPressed()
    {
        matchManager.StartNewMatch();
    }
}