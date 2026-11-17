using SecretFour.Localization;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DisconnectUIManager : MonoBehaviour
{
    [Header("Disconnect Panel")]
    [SerializeField] private GameObject disconnectPanel;
    [SerializeField] private TMP_Text messageText;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Mainmenu";

    private bool returningToMainMenu;
    private bool disconnectShown;
    private string messageKey;
    private void OnEnable() => GameLocalization.Changed += RefreshMessage;
    private void OnDisable() => GameLocalization.Changed -= RefreshMessage;
    private void RefreshMessage() { if (messageText != null && messageKey != null) messageText.text = GameLocalization.Text(messageKey); }

    public void ShowDisconnect(string message)
    {
        if (returningToMainMenu || disconnectShown)
            return;

        disconnectShown = true;

        Debug.Log($"DISCONNECT: {message}");

        if (disconnectPanel != null)
            disconnectPanel.SetActive(true);

        GameAudio.Play("error");

        if (messageText != null)
            messageText.text = GameLocalization.Text(message);
        messageKey = message;
    }

    /// <summary>
    /// Use this when the player intentionally leaves the online match.
    /// The remote player is informed before the local NetworkManager shuts down.
    /// </summary>
    public void LeaveOnlineMatch()
    {
        if (returningToMainMenu)
            return;

        NetworkDisconnectManager disconnectManager =
            FindFirstObjectByType<NetworkDisconnectManager>();

        if (disconnectManager != null)
            disconnectManager.NotifyIntentionalLeave();

        ReturnToMainMenu();
    }

    public void ReturnToMainMenu()
    {
        if (returningToMainMenu)
            return;

        returningToMainMenu = true;
        StartCoroutine(ReturnToMainMenuAfterShutdown());
    }

    private IEnumerator ReturnToMainMenuAfterShutdown()
    {
        Unity.Netcode.NetworkManager networkManager =
            Unity.Netcode.NetworkManager.Singleton;

        if (networkManager != null)
        {
            if (networkManager.IsListening || networkManager.ShutdownInProgress)
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
                Destroy(networkManager.gameObject);
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
