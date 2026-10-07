using SecretFour.Localization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MN7.Store;

public class GameHUD : MonoBehaviour
{
    [Header("Player Names")]
    [SerializeField] private TMP_Text playerXNameText;
    [SerializeField] private TMP_Text playerYNameText;

    [Header("Player Images")]
    [SerializeField] private Image playerXImage;
    [SerializeField] private Image playerYImage;
    [Tooltip("Fallback sprite used for Player X when no selected Store skin is available.")]
    [SerializeField] private Sprite playerXDefaultSprite;
    [Tooltip("Fallback sprite used for Player Y when no selected Store skin is available.")]
    [SerializeField] private Sprite playerYDefaultSprite;

    [Header("Turn Information")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text turnPlayerNameText;

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
    private bool hasConfiguredGameMode;
    private GameMode configuredGameMode;

    // Online cosmetics are owned by the network player. Cache the applied
    // sprite IDs so network/profile updates (including position swaps) do not
    // repeatedly reapply the same cosmetic UI.
    private string lastOnlineHostSkinId;
    private string lastOnlineJoinerSkinId;
    private bool onlineSkinsInitialized;

    private void Start()
    {
        SetupPlayers();
        SetupPlayerImages();
        SetupBotDifficulty();
        Refresh(true);
    }

    private void Update()
    {
        if (GameSession.Instance != null &&
            (!hasConfiguredGameMode ||
             configuredGameMode != GameSession.Instance.CurrentGameMode))
        {
            configuredGameMode = GameSession.Instance.CurrentGameMode;
            hasConfiguredGameMode = true;
            SetupBotDifficulty();
        }

        Refresh(false);
    }

    private void OnEnable()
    {
        GameLocalization.Changed += OnLanguageChanged;
        if (MN7StoreManager.Instance?.Selection != null)
            MN7StoreManager.Instance.Selection.OnSelectionChanged += OnStoreSelectionChanged;

        OnlineMatchPlayerState.OnAnyProfileChanged += OnOnlineProfileChanged;
    }

    private void OnDisable()
    {
        GameLocalization.Changed -= OnLanguageChanged;
        if (MN7StoreManager.Instance?.Selection != null)
            MN7StoreManager.Instance.Selection.OnSelectionChanged -= OnStoreSelectionChanged;

        OnlineMatchPlayerState.OnAnyProfileChanged -= OnOnlineProfileChanged;
    }

    private void OnLanguageChanged() { SetupBotDifficulty(); OnOnlineProfileChanged(); }

    private void OnOnlineProfileChanged()
    {
        SetupPlayers();
        Refresh(true);
    }

    private void OnStoreSelectionChanged(string selectionGroupId, string itemId)
    {
        if (string.Equals(selectionGroupId, SecretFourStoreIntegration.PlayerXSelectionGroup, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectionGroupId, SecretFourStoreIntegration.PlayerYSelectionGroup, System.StringComparison.OrdinalIgnoreCase))
        {
            SetupPlayerImages();
        }
    }

    private void SetupPlayers()
    {
        if (GameSession.Instance == null)
        {
            if (playerXNameText != null) playerXNameText.text = GameLocalization.Text("player.one");
            if (playerYNameText != null) playerYNameText.text = GameLocalization.Text("player.two");
            return;
        }

        if (GameSession.Instance.CurrentGameMode == GameMode.OnlineRoom &&
            OnlineMatchPlayerState.Instance != null &&
            OnlineMatchPlayerState.Instance.IsMatchReady())
        {
            // Online HUD uses a stable shared layout on both devices:
            // Host/X is always LEFT and Joiner/Y is always RIGHT.
            // Do not mirror the layout around the local player.
            PlayerType leftSide = PlayerType.X;
            PlayerType rightSide = PlayerType.Y;

            if (playerXNameText != null)
                playerXNameText.text =
                    GameLocalization.DisplayName(OnlineMatchPlayerState.Instance.GetName(leftSide));

            if (playerYNameText != null)
                playerYNameText.text =
                    GameLocalization.DisplayName(OnlineMatchPlayerState.Instance.GetName(rightSide));

            SetupOnlinePlayerImagesIfChanged(leftSide, rightSide);
            return;
        }

        if (playerXNameText != null)
            playerXNameText.text = GameLocalization.DisplayName(GameSession.Instance.Player1Name);

        if (playerYNameText != null)
            playerYNameText.text = GameLocalization.DisplayName(GameSession.Instance.Player2Name);
    }

    private void SetupOnlinePlayerImages(
        PlayerType leftSide,
        PlayerType rightSide)
    {
        Sprite leftSprite =
            SecretFourStoreIntegration.GetSpriteForItem(
                OnlineMatchPlayerState.Instance.GetSkinId(leftSide));

        Sprite rightSprite =
            SecretFourStoreIntegration.GetSpriteForItem(
                OnlineMatchPlayerState.Instance.GetSkinId(rightSide));

        if (leftSprite == null && coinConfig != null)
            leftSprite = coinConfig.GetSprite(leftSide);

        if (rightSprite == null && coinConfig != null)
            rightSprite = coinConfig.GetSprite(rightSide);

        if (playerXImage != null)
        {
            playerXImage.sprite = leftSprite;
            playerXImage.enabled = leftSprite != null;
        }

        if (playerYImage != null)
        {
            playerYImage.sprite = rightSprite;
            playerYImage.enabled = rightSprite != null;
        }
    }

    private void SetupOnlinePlayerImagesIfChanged(
        PlayerType leftSide,
        PlayerType rightSide)
    {
        if (OnlineMatchPlayerState.Instance == null)
            return;

        string hostSkinId =
            OnlineMatchPlayerState.Instance.GetSkinId(leftSide) ?? string.Empty;
        string joinerSkinId =
            OnlineMatchPlayerState.Instance.GetSkinId(rightSide) ?? string.Empty;

        if (onlineSkinsInitialized &&
            string.Equals(lastOnlineHostSkinId, hostSkinId, System.StringComparison.Ordinal) &&
            string.Equals(lastOnlineJoinerSkinId, joinerSkinId, System.StringComparison.Ordinal))
        {
            return;
        }

        SetupOnlinePlayerImages(leftSide, rightSide);
        lastOnlineHostSkinId = hostSkinId;
        lastOnlineJoinerSkinId = joinerSkinId;
        onlineSkinsInitialized = true;
    }

    private void SetupPlayerImages()
    {
        if (GameSession.Instance != null &&
            GameSession.Instance.CurrentGameMode == GameMode.OnlineRoom &&
            OnlineMatchPlayerState.Instance != null &&
            OnlineMatchPlayerState.Instance.IsMatchReady())
        {
            // Keep the online HUD identical on both devices.
            SetupOnlinePlayerImagesIfChanged(
                PlayerType.X,
                PlayerType.Y);

            return;
        }

        Sprite xSprite = SecretFourStoreIntegration.GetSelectedPlayerSprite(PlayerType.X);
        Sprite ySprite = SecretFourStoreIntegration.GetSelectedPlayerSprite(PlayerType.Y);

        if (xSprite == null && coinConfig != null)
            xSprite = coinConfig.GetSprite(PlayerType.X);

        if (ySprite == null && coinConfig != null)
            ySprite = coinConfig.GetSprite(PlayerType.Y);

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
            Debug.LogWarning("GameHUD player coin icons are not assigned. Set Store selections or X/Y fallback sprites.");
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

        string activeName = turnManager.CurrentState == GameState.WaitingForPlayerXPlacement || turnManager.CurrentState == GameState.WaitingForPlayerXDirection ? GetPlayerXName() :
            turnManager.CurrentState == GameState.WaitingForPlayerYPlacement || turnManager.CurrentState == GameState.WaitingForPlayerYDirection ? GetPlayerYName() : null;
        if (turnPlayerNameText != null)
        {
            turnPlayerNameText.text = GameLocalization.DisplayName(activeName) ?? string.Empty;
            turnPlayerNameText.gameObject.SetActive(activeName != null);
            turnText.rectTransform.anchoredPosition = new Vector2(activeName == null ? 0 : 155, -55);
            turnText.rectTransform.sizeDelta = new Vector2(activeName == null ? 810 : 500, 88);
        }
        switch (turnManager.CurrentState)
        {
            case GameState.WaitingForPlayerXPlacement:
                turnText.text = GameLocalization.Text("game.turn");
                break;
            case GameState.WaitingForPlayerXDirection:
                turnText.text = GameLocalization.Text("game.direction");
                break;
            case GameState.WaitingForPlayerYPlacement:
                turnText.text = GameLocalization.Text("game.turn");
                break;
            case GameState.WaitingForPlayerYDirection:
                turnText.text = GameLocalization.Text("game.direction");
                break;
            // These are internal resolution phases. They remain part of
            // the authoritative game state, but are intentionally not exposed
            // as player-facing turn messages.
            case GameState.RevealingDirections:
            case GameState.Moving:
                turnText.text = GameLocalization.Format("game.reveal", GameLocalization.Text("direction." + turnManager.PlayerXAction.direction.ToString().ToLowerInvariant()), GameLocalization.Text("direction." + turnManager.PlayerYAction.direction.ToString().ToLowerInvariant()));
                break;
            case GameState.CheckingResult:
                turnText.text = GameLocalization.Text("game.checking");
                break;
            case GameState.Won:
                turnText.text = GameLocalization.Text("game.over");
                break;
            case GameState.Draw:
                turnText.text = GameLocalization.Text("game.draw");
                break;
            default:
                turnText.text = "";
                break;
        }
    }

    private void UpdatePlayerHighlight()
    {
        GameState state = turnManager.CurrentState;

        PlayerType? activeSide =
            state == GameState.WaitingForPlayerXPlacement ||
            state == GameState.WaitingForPlayerXDirection
                ? PlayerType.X
                : state == GameState.WaitingForPlayerYPlacement ||
                  state == GameState.WaitingForPlayerYDirection
                    ? PlayerType.Y
                    : (PlayerType?)null;

        if (GameSession.Instance != null &&
            GameSession.Instance.CurrentGameMode == GameMode.OnlineRoom &&
            OnlineMatchPlayerState.Instance != null &&
            OnlineMatchPlayerState.Instance.IsMatchReady())
        {
            // X is always the left player and Y is always the right
            // player in the shared online room UI.
            PlayerType leftSide = PlayerType.X;
            PlayerType rightSide = PlayerType.Y;

            if (activeSide == leftSide)
            {
                SetImageAlpha(playerXImage, activeAlpha);
                SetTextAlpha(playerXNameText, activeAlpha);
                SetImageAlpha(playerYImage, inactiveAlpha);
                SetTextAlpha(playerYNameText, inactiveAlpha);
            }
            else if (activeSide == rightSide)
            {
                SetImageAlpha(playerXImage, inactiveAlpha);
                SetTextAlpha(playerXNameText, inactiveAlpha);
                SetImageAlpha(playerYImage, activeAlpha);
                SetTextAlpha(playerYNameText, activeAlpha);
            }
            else
            {
                SetImageAlpha(playerXImage, activeAlpha);
                SetTextAlpha(playerXNameText, activeAlpha);
                SetImageAlpha(playerYImage, activeAlpha);
                SetTextAlpha(playerYNameText, activeAlpha);
            }

            return;
        }

        bool xActive = activeSide == PlayerType.X;
        bool yActive = activeSide == PlayerType.Y;

        if (xActive)
        {
            SetImageAlpha(playerXImage, activeAlpha);
            SetTextAlpha(playerXNameText, activeAlpha);
            SetImageAlpha(playerYImage, inactiveAlpha);
            SetTextAlpha(playerYNameText, inactiveAlpha);
        }
        else if (yActive)
        {
            SetImageAlpha(playerXImage, inactiveAlpha);
            SetTextAlpha(playerXNameText, inactiveAlpha);
            SetImageAlpha(playerYImage, activeAlpha);
            SetTextAlpha(playerYNameText, activeAlpha);
        }
        else
        {
            SetImageAlpha(playerXImage, activeAlpha);
            SetTextAlpha(playerXNameText, activeAlpha);
            SetImageAlpha(playerYImage, activeAlpha);
            SetTextAlpha(playerYNameText, activeAlpha);
        }
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
            return;

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private string GetPlayerXName()
    {
        if (GameSession.Instance != null &&
            GameSession.Instance.CurrentGameMode == GameMode.OnlineRoom &&
            OnlineMatchPlayerState.Instance != null &&
            OnlineMatchPlayerState.Instance.IsMatchReady())
        {
            return OnlineMatchPlayerState.Instance.GetName(PlayerType.X);
        }

        return GameSession.Instance != null
            ? GameSession.Instance.Player1Name
            : "Player 1";
    }

    private string GetPlayerYName()
    {
        if (GameSession.Instance != null &&
            GameSession.Instance.CurrentGameMode == GameMode.OnlineRoom &&
            OnlineMatchPlayerState.Instance != null &&
            OnlineMatchPlayerState.Instance.IsMatchReady())
        {
            return OnlineMatchPlayerState.Instance.GetName(PlayerType.Y);
        }

        return GameSession.Instance != null
            ? GameSession.Instance.Player2Name
            : "Player 2";
    }

    private void SetupBotDifficulty()
    {
        if (botDifficultyText == null)
            return;

        botDifficultyText.gameObject.SetActive(false);

        if (GameSession.Instance == null)
            return;

        if (GameSession.Instance.CurrentGameMode != GameMode.Bot)
            return;

        BotDifficulty difficulty = GameSession.Instance.CurrentBotDifficulty;
        botDifficultyText.gameObject.SetActive(true);

        switch (difficulty)
        {
            case BotDifficulty.Easy:
                botDifficultyText.text = GameLocalization.Text("bot.easy");
                break;
            case BotDifficulty.Medium:
                botDifficultyText.text = GameLocalization.Text("bot.medium");
                break;
            case BotDifficulty.Hard:
                botDifficultyText.text = GameLocalization.Text("bot.hard");
                break;
            default:
                botDifficultyText.text = GameLocalization.Text("player.bot");
                break;
        }
    }
}