using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private GameResultManager gameResultManager;

    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private TMP_Text resultTitle;

    [SerializeField]
    private TMP_Text resultSubtitle;

    [SerializeField]
    private GameOverUI gameOverUI;

    private void Start()
    {
        Hide();
    }

    public void ShowWinner(
        PlayerType winner)
    {
        panel.SetActive(true);

        resultTitle.text =
            $"{winner} WINS!";

        resultSubtitle.text =
            "Four in a row!";
    }

    public void ShowDraw()
    {
        panel.SetActive(true);

        resultTitle.text =
            "DRAW";

        resultSubtitle.text =
            "No winner this time.";
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}