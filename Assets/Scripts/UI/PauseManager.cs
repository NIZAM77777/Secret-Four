using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]

    [SerializeField]
    private GameObject pausePanel;

    [Header("Scenes")]

    [SerializeField]
    private string mainMenuSceneName = "Mainmenu";

    private bool isPaused;
    private bool isBlockingInput;
    private bool returningToMainMenu;

    /// <summary>
    /// Online Room matches must not expose local pause UI. A player pausing one
    /// device would otherwise imply that the shared network match is paused.
    /// </summary>
    private bool IsOnlineRoom()
    {
        return GameSession.Instance != null &&
               GameSession.Instance.CurrentGameMode == GameMode.OnlineRoom;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isBlockingInput = false;
        returningToMainMenu = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Room games are controlled by the authoritative network match and
        // intentionally have no pause button/panel presentation.
        if (IsOnlineRoom())
            enabled = false;
    }

    public void PauseGame()
    {
        if (isPaused || returningToMainMenu)
            return;

        isPaused = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        Time.timeScale = 0f;
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (!isPaused || returningToMainMenu)
            return;

        isBlockingInput = true;
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        StartCoroutine(WaitForInputRelease());
        Debug.Log("Game Resumed");
    }

    private IEnumerator WaitForInputRelease()
    {
        yield return null;

        while (Mouse.current != null &&
               Mouse.current.leftButton.isPressed)
        {
            yield return null;
        }

        yield return null;
        isBlockingInput = false;

        Debug.Log("Board Input Unlocked");
    }

    public void TogglePause()
    {
        if (returningToMainMenu)
            return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public bool IsPaused() => isPaused;

    public bool IsInputBlocked() => isBlockingInput;

    public void RestartGame()
    {
        if (returningToMainMenu)
            return;

        Time.timeScale = 1f;
        isPaused = false;
        isBlockingInput = false;

        // Never reload the online game scene locally. That would reset one
        // device independently from the authoritative match.
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            NetworkPlayAgainManager playAgain =
                FindFirstObjectByType<NetworkPlayAgainManager>();

            if (playAgain != null)
            {
                Debug.Log(
                    "ONLINE RESTART: requesting synchronized rematch."
                );

                playAgain.RequestPlayAgain();
            }
            else
            {
                Debug.LogWarning(
                    "ONLINE RESTART: NetworkPlayAgainManager is missing."
                );
            }

            return;
        }

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void ReturnToMainMenu()
    {
        if (returningToMainMenu)
            return;

        returningToMainMenu = true;
        Time.timeScale = 1f;
        isPaused = false;
        isBlockingInput = false;

        StartCoroutine(ReturnToMainMenuAfterShutdown());
    }

    private IEnumerator ReturnToMainMenuAfterShutdown()
    {
        Unity.Netcode.NetworkManager networkManager =
            Unity.Netcode.NetworkManager.Singleton;

        if (networkManager != null)
        {
            if (networkManager.IsListening ||
                networkManager.ShutdownInProgress)
            {
                networkManager.Shutdown(true);

                while (Unity.Netcode.NetworkManager.Singleton != null &&
                       (Unity.Netcode.NetworkManager.Singleton.IsListening ||
                        Unity.Netcode.NetworkManager.Singleton.ShutdownInProgress))
                {
                    yield return null;
                }
            }

            if (networkManager != null)
            {
                Destroy(networkManager.gameObject);
            }
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}