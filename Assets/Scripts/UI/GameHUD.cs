using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Player Names")]

    [SerializeField]
    private TMP_Text playerXNameText;

    [SerializeField]
    private TMP_Text playerYNameText;

    [Header("Player Images")]

    [SerializeField]
    private Image playerXImage;

    [SerializeField]
    private Image playerYImage;

    [Header("Turn Information")]

    [SerializeField]
    private TMP_Text turnText;

    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private CoinConfig coinConfig;

    [Header("Visual Settings")]

    [SerializeField]
    private float activeAlpha = 1f;

    [SerializeField]
    private float inactiveAlpha = 0.35f;

    private void Start()
    {
        SetupPlayers();
        SetupPlayerImages();

        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    // =========================
    // PLAYER SETUP
    // =========================

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
    }

    // =========================
    // PLAYER IMAGES
    // =========================

    private void SetupPlayerImages()
    {
        if (coinConfig == null)
        {
            Debug.LogWarning(
                "CoinConfig is missing in GameHUD."
            );

            return;
        }

        if (playerXImage != null)
        {
            playerXImage.sprite =
                coinConfig.GetSprite(PlayerType.X);
        }

        if (playerYImage != null)
        {
            playerYImage.sprite =
                coinConfig.GetSprite(PlayerType.Y);
        }
    }

    // =========================
    // HUD REFRESH
    // =========================

    private void Refresh()
    {
        if (turnManager == null)
        {
            return;
        }

        UpdateTurnText();
        UpdatePlayerHighlight();
    }

    // =========================
    // TURN TEXT
    // =========================

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
                    "Checking Result...";

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

                turnText.text = "";

                break;
        }
    }

    // =========================
    // ACTIVE PLAYER VISUAL
    // =========================

    private void UpdatePlayerHighlight()
    {
        GameState state =
            turnManager.CurrentState;

        bool xActive =
            state ==
                GameState.WaitingForPlayerXPlacement
            ||
            state ==
                GameState.WaitingForPlayerXDirection;

        bool yActive =
            state ==
                GameState.WaitingForPlayerYPlacement
            ||
            state ==
                GameState.WaitingForPlayerYDirection;

        if (xActive)
        {
            SetImageAlpha(
                playerXImage,
                activeAlpha
            );

            SetImageAlpha(
                playerYImage,
                inactiveAlpha
            );
        }
        else if (yActive)
        {
            SetImageAlpha(
                playerXImage,
                inactiveAlpha
            );

            SetImageAlpha(
                playerYImage,
                activeAlpha
            );
        }
        else
        {
            // During reveal/movement:
            // both players visible normally.

            SetImageAlpha(
                playerXImage,
                activeAlpha
            );

            SetImageAlpha(
                playerYImage,
                activeAlpha
            );
        }
    }

    // =========================
    // IMAGE ALPHA
    // =========================

    private void SetImageAlpha(
        Image image,
        float alpha)
    {
        if (image == null)
            return;

        Color color =
            image.color;

        color.a = alpha;

        image.color = color;
    }

    // =========================
    // PLAYER NAMES
    // =========================

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