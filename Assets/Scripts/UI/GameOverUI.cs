using System.Collections;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private TMP_Text resultTitle;

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


    private bool returningToMainMenu;


    private void Start()
    {
        returningToMainMenu = false;
        Hide();
    }


    public void SetWaitingForOpponent(bool waiting)
    {
        if (!waiting || !IsOnlineGame())
            return;

        if (resultTitle != null)
            resultTitle.text = "WAITING FOR OPPONENT...";
    }


    public void ShowWinner(PlayerType winner)
    {
        if (panel != null)
            panel.SetActive(true);

        string winnerName = GetPlayerName(winner);

        if (resultTitle != null)
            resultTitle.text = $"{winnerName} WINS!";

        SetupWinnerImage(winner);
    }


    public void ShowDraw()
    {
        if (panel != null)
            panel.SetActive(true);

        if (resultTitle != null)
            resultTitle.text = "DRAW!";

        if (winnerImage != null)
            winnerImage.gameObject.SetActive(false);
    }


    private string GetPlayerName(PlayerType player)
    {
        if (GameSession.Instance != null &&
            GameSession.Instance.CurrentGameMode == GameMode.OnlineRoom &&
            OnlineMatchPlayerState.Instance != null &&
            OnlineMatchPlayerState.Instance.IsMatchReady())
        {
            return OnlineMatchPlayerState.Instance.GetName(player);
        }

        if (GameSession.Instance != null)
        {
            return player == PlayerType.X
                ? GameSession.Instance.Player1Name
                : GameSession.Instance.Player2Name;
        }

        return player == PlayerType.X ? "Player 1" : "Player 2";
    }


    private void SetupWinnerImage(PlayerType winner)
    {
        if (winnerImage == null)
        {
            Debug.LogWarning("GameOverUI: Winner Image is not assigned.");
            return;
        }

        Sprite sprite = null;

        // Online matches must use the winning network player's skin.
        // Local/Bot matches can use the local Store selection.
        if (GameSession.Instance != null &&
            GameSession.Instance.CurrentGameMode == GameMode.OnlineRoom &&
            OnlineMatchPlayerState.Instance != null &&
            OnlineMatchPlayerState.Instance.IsMatchReady())
        {
            string skinId =
                OnlineMatchPlayerState.Instance.GetSkinId(winner);

            sprite = SecretFourStoreIntegration.GetSpriteForItem(skinId);
        }
        else
        {
            sprite = SecretFourStoreIntegration.GetSelectedPlayerSprite(winner);
        }

        // Fall back to the original CoinConfig sprite when no store skin is selected.
        if (sprite == null && coinConfig != null)
            sprite = coinConfig.GetSprite(winner);

        winnerImage.sprite = sprite;
        winnerImage.preserveAspect = true;
        winnerImage.gameObject.SetActive(sprite != null);

        if (sprite == null)
            Debug.LogWarning($"GameOverUI: No winner sprite available for {winner}.");
    }


    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }


    public void OnPlayAgainPressed()
    {
        if (IsOnlineGame())
        {
            if (networkPlayAgainManager == null)
            {
                Debug.LogError(
                    "NetworkPlayAgainManager is missing."
                );
                return;
            }

            Debug.Log("ONLINE → PLAY AGAIN REQUESTED");
            networkPlayAgainManager.RequestPlayAgain();
            return;
        }

        if (matchManager != null)
        {
            Debug.Log("LOCAL → STARTING NEW MATCH");
            matchManager.StartNewMatch();
        }
    }


    private bool IsOnlineGame()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening;
    }


    public void OnMainMenuPressed()
    {
        if (returningToMainMenu)
            return;

        returningToMainMenu = true;

        Debug.Log("MAIN MENU BUTTON PRESSED");

        StartCoroutine(ReturnToMainMenuAfterNetworkShutdown());
    }


    private IEnumerator ReturnToMainMenuAfterNetworkShutdown()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null)
        {
            if (networkManager.IsListening ||
                networkManager.ShutdownInProgress)
            {
                Debug.Log(
                    "Shutting down network before returning to Main Menu..."
                );

                networkManager.Shutdown(true);

                while (NetworkManager.Singleton != null &&
                       (NetworkManager.Singleton.IsListening ||
                        NetworkManager.Singleton.ShutdownInProgress))
                {
                    yield return null;
                }
            }

            // NetworkManager is a persistent singleton. Destroy it before
            // loading Mainmenu so that Mainmenu can create its fresh instance
            // without producing a duplicate DDOL NetworkManager.
            if (networkManager != null)
            {
                Destroy(networkManager.gameObject);
            }
        }

        SceneManager.LoadScene("Mainmenu");
    }
}