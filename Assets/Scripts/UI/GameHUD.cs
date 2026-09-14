using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Player Names")]
    [SerializeField] private TMP_Text playerXNameText;
    [SerializeField] private TMP_Text playerYNameText;

    [Header("Player Images")]
    [SerializeField] private Image playerXImage;
    [SerializeField] private Image playerYImage;

    [Tooltip("Fallback sprite used for Player X when CoinConfig does not provide one.")]
    [SerializeField] private Sprite playerXDefaultSprite;

    [Tooltip("Fallback sprite used for Player Y when CoinConfig does not provide one.")]
    [SerializeField] private Sprite playerYDefaultSprite;

    [Header("Turn Information")]
    [SerializeField] private TMP_Text turnText;

    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private CoinConfig coinConfig;

    [Header("Visual Settings")]
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private float inactiveAlpha = 0.35f;

    [Header("Bot Difficulty")]
    [SerializeField] private TMP_Text botDifficultyText;

    private GameState lastState;
    private bool hasLastState;

    private void Start()
    {
        SetupPlayers();
        SetupPlayerImages();
        SetupBotDifficulty();
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void SetupPlayers()
    {
        if (GameSession.Instance == null)
        {
            if (playerXNameText != null) playerXNameText.text = "Player 1";
            if (playerYNameText != null) playerYNameText.text = "Player 2";
            return;
        }

        if (playerXNameText != null)
            playerXNameText.text = GameSession.Instance.Player1Name;

        if (playerYNameText != null)
            playerYNameText.text = GameSession.Instance.Player2Name;
    }

    private void SetupPlayerImages()
    {
        Sprite xSprite = null;
        Sprite ySprite = null;

        if (coinConfig != null)
        {
            xSprite = coinConfig.GetSprite(PlayerType.X);
            ySprite = coinConfig.GetSprite(PlayerType.Y);
        }

        // CoinConfig is the primary source. Explicit HUD fallbacks make the header
        // remain visible even when the CoinConfig asset has no sprite assigned.
        if (xSprite == null)
            xSprite = playerXDefaultSprite;

        if (ySprite == null)
            ySprite = playerYDefaultSprite;

        if (playerXImage != null)
        {
            playerXImage.sprite = xSprite;
            playerXImage.enabled = xSprite != null;
        }

        if (playerYImage != null)
        {
            playerYImage.sprite = ySprite;
            playerYImage.enabled = ySprite != null;
        }

        if (xSprite == null || ySprite == null)
        {
            Debug.LogWarning(
                "GameHUD player coin icons are not assigned. Set X/Y sprites in CoinConfig or assign the HUD fallback sprites."
            );
        }
    }

    private void Refresh(bool force)
    {
        if (turnManager == null)
            return;

        GameState currentState = turnManager.CurrentState;

        if (!force && hasLastState && currentState == lastState)
            return;

        lastState = currentState;
        hasLastState = true;

        UpdateTurnText();
        UpdatePlayerHighlight();
    }

    private void UpdateTurnText()
    {
        if (turnText == null)
            return;

        switch (turnManager.CurrentState)
        {
            case GameState.WaitingForPlayerXPlacement:
                turnText.text = $"{GetPlayerXName()}'s Turn";
                break;
            case GameState.WaitingForPlayerXDirection:
                turnText.text = $"{GetPlayerXName()} — Choose Direction";
                break;
            case GameState.WaitingForPlayerYPlacement:
                turnText.text = $"{GetPlayerYName()}'s Turn";
                break;
            case GameState.WaitingForPlayerYDirection:
                turnText.text = $"{GetPlayerYName()} — Choose Direction";
                break;
            case GameState.RevealingDirections:
                turnText.text = "Directions Revealed";
                break;
            case GameState.Moving:
                turnText.text = "Moving...";
                break;
            case GameState.CheckingResult:
                turnText.text = "Checking Result...";
                break;
            case GameState.Won:
                turnText.text = "Game Over";
                break;
            case GameState.Draw:
                turnText.text = "Draw";
                break;
            default:
                turnText.text = "";
                break;
        }
    }
