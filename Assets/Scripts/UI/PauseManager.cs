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

    private void Start()
    {
        Time.timeScale = 1f;

        isPaused = false;
        isBlockingInput = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Time.timeScale = 0f;

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        // Block board input BEFORE resuming.
        isBlockingInput = true;

        isPaused = false;

        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        StartCoroutine(
            WaitForInputRelease()
        );

        Debug.Log("Game Resumed");
    }

    private IEnumerator WaitForInputRelease()
    {
        // Wait at least one frame.
        yield return null;

        // Wait until mouse button is released.
        while (Mouse.current != null &&
               Mouse.current.leftButton.isPressed)
        {
            yield return null;
        }

        // Small safety frame.
        yield return null;

        isBlockingInput = false;

        Debug.Log("Board Input Unlocked");
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    public bool IsInputBlocked()
    {
        return isBlockingInput;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        isPaused = false;
        isBlockingInput = false;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        isPaused = false;
        isBlockingInput = false;

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }
}