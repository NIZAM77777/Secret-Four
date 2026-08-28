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

    [Header("Bot Difficulty")]

    [SerializeField]
    private TMP_Text botDifficultyText;


    private void Start()
    {
        SetupPlayers();

        SetupPlayerImages();

        SetupBotDifficulty();

        Refresh();
    }


    private void Update()
    {
        Refresh();
    }


    // =================================================
    // PLAYER SETUP
    // =================================================

    private void SetupPlayers()
    {
        if (GameSession.Instance == null)
        {
            if (playerXNameText != null)
            {
                playerXNameText.text =
                    "Player 1";
            }

            if (playerYNameText != null)
            {
                playerYNameText.text =
                    "Player 2";
            }

            return;
        }

        if (playerXNameText != null)
        {
            playerXNameText.text =
                GameSession.Instance.Player1Name;
        }

        if (playerYNameText != null)
        {
            playerYNameText.text =
                GameSession.Instance.Player2Name;
        }
    }


    // =================================================
    // PLAYER IMAGES
    // =================================================

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
                coinConfig.GetSprite(
                    PlayerType.X
                );
        }

        if (playerYImage != null)
        {
            playerYImage.sprite =
                coinConfig.GetSprite(
                    PlayerType.Y
                );
        }
    }


    // =================================================
    // HUD REFRESH
    // =================================================

    private void Refresh()
    {
        if (turnManager == null)
        {
            return;
        }

        UpdateTurnText();

        UpdatePlayerHighlight();
    }


    // =================================================
    // TURN TEXT
    // =================================================

    private void UpdateTurnText()
    {
        if (turnText == null)
        {
            return;
        }

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


    // =================================================
    // ACTIVE PLAYER VISUAL
    // =================================================

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


        // ---------------------------------------------
        // X ACTIVE
        // ---------------------------------------------

        if (xActive)
        {
            SetImageAlpha(
                playerXImage,
                activeAlpha
            );

            SetTextAlpha(
                playerXNameText,
                activeAlpha
            );


            SetImageAlpha(
                playerYImage,
                inactiveAlpha
            );

            SetTextAlpha(
                playerYNameText,
                inactiveAlpha
            );
        }


        // ---------------------------------------------
        // Y ACTIVE
        // ---------------------------------------------

        else if (yActive)
        {
            SetImageAlpha(
                playerXImage,
                inactiveAlpha
            );

            SetTextAlpha(
                playerXNameText,
                inactiveAlpha
            );


            SetImageAlpha(
                playerYImage,
                activeAlpha
            );

            SetTextAlpha(
                playerYNameText,
                activeAlpha
            );
        }


        // ---------------------------------------------
        // REVEAL / MOVEMENT
        // ---------------------------------------------

        else
        {
            SetImageAlpha(
                playerXImage,
                activeAlpha
            );

            SetTextAlpha(
                playerXNameText,
                activeAlpha
            );


            SetImageAlpha(
                playerYImage,
                activeAlpha
            );

            SetTextAlpha(
                playerYNameText,
                activeAlpha
            );
        }
    }


    // =================================================
    // IMAGE ALPHA
    // =================================================

    private void SetImageAlpha(
        Image image,
        float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color =
            image.color;

        color.a = alpha;

        image.color = color;
    }


    // =================================================
    // TEXT ALPHA
    // =================================================

    private void SetTextAlpha(
        TMP_Text text,
        float alpha)
    {
        if (text == null)
        {
            return;
        }

        Color color =
            text.color;

        color.a = alpha;

        text.color = color;
    }


    // =================================================
    // PLAYER NAMES
    // =================================================

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

    // =========================
    // BOT DIFFICULTY
    // =========================

    private void SetupBotDifficulty()
    {
        if (botDifficultyText == null)
            return;

        // Hide by default
        botDifficultyText.gameObject.SetActive(false);

        if (GameSession.Instance == null)
            return;

        // Only show in Bot mode
        if (GameSession.Instance.CurrentGameMode !=
            GameMode.Bot)
        {
            return;
        }

        BotDifficulty difficulty =
            GameSession.Instance.CurrentBotDifficulty;

        botDifficultyText.gameObject.SetActive(true);

        switch (difficulty)
        {
            case BotDifficulty.Easy:

                botDifficultyText.text =
                    "Easy";

                break;

            case BotDifficulty.Medium:

                botDifficultyText.text =
                    "Medium";

                break;

            case BotDifficulty.Hard:

                botDifficultyText.text =
                    "Hard";

                break;

            default:

                botDifficultyText.text =
                    "🤖 Bot";

                break;
        }
    }
}