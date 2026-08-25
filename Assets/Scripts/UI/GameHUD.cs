using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Player Names")]

    [SerializeField]
    private TMP_Text playerXNameText;

    [SerializeField]
    private TMP_Text playerYNameText;

    [Header("Player Labels")]

    [SerializeField]
    private TMP_Text playerXLabelText;

    [SerializeField]
    private TMP_Text playerYLabelText;

    [Header("Turn Information")]

    [SerializeField]
    private TMP_Text turnText;

    [Header("Collision")]

    [SerializeField]
    private TMP_Text collisionText;

    [SerializeField]
    private GameConfig gameConfig;

    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    private void Start()
    {
        SetupPlayers();

        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void SetupPlayers()
    {
        if (GameSession.Instance == null)
        {
            playerXNameText.text = "Player 1";
            playerYNameText.text = "Player 2";

            return;
        }

        playerXNameText.text =
            GameSession.Instance.Player1Name;

        playerYNameText.text =
            GameSession.Instance.Player2Name;

        playerXLabelText.text = "X";
        playerYLabelText.text = "Y";
    }

    private void Refresh()
    {
        if (turnManager == null)
        {
            return;
        }

        UpdateTurnText();
        UpdateCollisionText();
    }

    private void UpdateTurnText()
    {
        switch (turnManager.CurrentState)
        {
            case GameState.WaitingForPlayerXPlacement:

                turnText.text =
                    $"{GetPlayerXName()}'s Turn";

                break;

            case GameState.WaitingForPlayerXDirection:

                turnText.text =
                    $"{GetPlayerXName()} — Choose Direction";

                break;

            case GameState.WaitingForPlayerYPlacement:

                turnText.text =
                    $"{GetPlayerYName()}'s Turn";

                break;

            case GameState.WaitingForPlayerYDirection:

                turnText.text =
                    $"{GetPlayerYName()} — Choose Direction";

                break;

            case GameState.RevealingDirections:

                turnText.text =
                    "Directions Revealed";

                break;

            case GameState.Moving:

                turnText.text =
                    "Moving...";

                break;

            case GameState.CheckingResult:

                turnText.text =
                    "Checking...";

                break;

            case GameState.Won:

                turnText.text =
                    "Game Over";

                break;

            case GameState.Draw:

                turnText.text =
                    "Draw";

                break;

            default:

                turnText.text =
                    "";

                break;
        }
    }

    private void UpdateCollisionText()
    {
        collisionText.text =
            $"Collisions  {turnManager.CollisionCount} / " +
            $"{gameConfig.collisionDrawCount}";
    }

    private string GetPlayerXName()
    {
        if (GameSession.Instance == null)
        {
            return "Player 1";
        }

        return GameSession.Instance.Player1Name;
    }

    private string GetPlayerYName()
    {
        if (GameSession.Instance == null)
        {
            return "Player 2";
        }

        return GameSession.Instance.Player2Name;
    }
}