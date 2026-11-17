using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// App-wide runtime settings, created automatically at startup (no scene setup needed):
/// - 60 FPS on phones (Android otherwise defaults to 30).
/// - Screen never dims/sleeps while the game is open.
/// - Release builds only print warnings and errors (Debug.Log is skipped).
/// - Android Back button closes the open panel, pauses a match, or sends the app to background on the main menu.
/// </summary>
public class AppRuntimeSetup : MonoBehaviour
{
    // Checked top to bottom: the first open panel wins, so overlays come before what they cover.
    // A null button means "Back does nothing here" (the player must use an on-screen choice).
    private static readonly (string panel, string button)[] BackRoutes =
    {
        // Game scene
        ("SettingsPanel", "Back"),
        ("Pausepanel", "ResumeButton"),
        ("DisconnectPanel", null),
        ("GameOverPanel", null),
        // Main menu
        ("StorePanel", "CloseButton"),
        ("RulesPanel", "BackToMainmenu"),
        ("BotSelectionPanel", "BackButton"),
        ("HumanSetupPanel", "BackToMainmenu"),
        ("CreatingRoomPanel", "Cancel creation"),
        ("Createroompanel", "CancelButton"),
        ("JoinRoomPanel", "CloseButton"),
        ("PlayInRoomPanel", "BackToMainmenu"),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (!Debug.isDebugBuild && !Application.isEditor)
            Debug.unityLogger.filterLogType = LogType.Warning;

        GameObject go = new("App Runtime Setup");
        DontDestroyOnLoad(go);
        go.AddComponent<AppRuntimeSetup>();
    }

    private void Update()
    {
        if (BackPressed())
            HandleBack();
    }

    private static bool BackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private static void HandleBack()
    {
        foreach ((string panelName, string buttonName) in BackRoutes)
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel == null || !panel.activeInHierarchy)
                continue;

            if (buttonName == null)
                return;

            if (TryClick(panel.transform, buttonName))
                return;
        }

        // Match in progress: open the pause menu (pause is disabled for online rooms by PauseManager).
        GameObject pause = GameObject.Find("PauseButton");
        if (pause != null && TryClick(pause.transform.parent, "PauseButton"))
            return;

        if (GameObject.Find("MainmenuPanel") != null)
            MoveAppToBackground();
    }

    private static bool TryClick(Transform root, string name)
    {
        Transform target = FindDeep(root, name);
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        Button button = target.GetComponent<Button>();
        if (button == null || !button.IsInteractable())
            return false;

        button.onClick.Invoke();
        return true;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static void MoveAppToBackground()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using AndroidJavaClass player = new("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call<bool>("moveTaskToBack", true);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[App] Could not move app to background: {exception.Message}");
        }
#endif
    }
}
