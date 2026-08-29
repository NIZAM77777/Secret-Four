using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

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

    [Header("Online Multiplayer")]

    [SerializeField]
    private NetworkPlayAgainManager networkPlayAgainManager;

    [Header("Winner Visual")]

    [SerializeField]
    private Image winnerImage;

    [SerializeField]
    private CoinConfig coinConfig;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        Hide();
    }


    // =====================================================
    // SHOW WINNER
    // =====================================================

    public void ShowWinner(
        PlayerType winner)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        string winnerName =
            GetPlayerName(winner);

        if (resultTitle != null)
        {
            resultTitle.text =
                $"{winnerName} WINS!";
        }

        if (resultSubtitle != null)
        {
            resultSubtitle.text =
                "Four in a row!";
        }

        SetupWinnerImage(winner);
    }


    // =====================================================
    // SHOW DRAW
    // =====================================================

    public void ShowDraw()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (resultTitle != null)
        {
            resultTitle.text =
                "DRAW!";
        }

        if (resultSubtitle != null)
        {
            resultSubtitle.text =
                "No winner this time.";
        }

        if (winnerImage != null)
        {
            winnerImage.gameObject.SetActive(false);
        }
    }


    // =====================================================
    // GET PLAYER NAME
    // =====================================================

    private string GetPlayerName(
        PlayerType player)
    {
        if (GameSession.Instance == null)
        {
            return player == PlayerType.X
                ? "Player 1"
                : "Player 2";
        }

        if (player == PlayerType.X)
        {
            return GameSession.Instance.Player1Name;
        }

        return GameSession.Instance.Player2Name;
    }


    // =====================================================
    // SETUP WINNER IMAGE
    // =====================================================

    private void SetupWinnerImage(
        PlayerType winner)
    {
        if (winnerImage == null)
            return;

        if (coinConfig == null)
            return;

        winnerImage.gameObject.SetActive(true);

        winnerImage.sprite =
            coinConfig.GetSprite(winner);
    }


    // =====================================================
    // HIDE
    // =====================================================

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }


    // =====================================================
    // PLAY AGAIN
    // =====================================================

    public void OnPlayAgainPressed()
    {
        // =============================================
        // ONLINE GAME
        // =============================================

        if (IsOnlineGame())
        {
            if (networkPlayAgainManager == null)
            {
                Debug.LogError(
                    "NetworkPlayAgainManager is missing."
                );

                return;
            }

            Debug.Log(
                "ONLINE → PLAY AGAIN REQUESTED"
            );

            networkPlayAgainManager
                .RequestPlayAgain();

            return;
        }


        // =============================================
        // LOCAL / BOT GAME
        // =============================================

        if (matchManager != null)
        {
            Debug.Log(
                "LOCAL → STARTING NEW MATCH"
            );

            matchManager.StartNewMatch();
        }
    }


    // =====================================================
    // ONLINE CHECK
    // =====================================================

    private bool IsOnlineGame()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening;
    }
}