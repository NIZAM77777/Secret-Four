#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SecretFourFlowValidation
{
    private const string Running = "SecretFour.FlowValidation";
    static SecretFourFlowValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Running, false))
            {
                SessionState.SetBool(Running, false);
                new GameObject("Secret Four flow validation").AddComponent<SecretFourFlowProbe>();
            }
        };
    }
    public static void RunBatch()
    {
        SecretFourValidation.RunAllChecks();
        SetPortraitGameView();
        SessionState.SetBool(Running, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Mainmenu.unity");
        EditorApplication.EnterPlaymode();
    }
    private static void SetPortraitGameView()
    {
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var kindType = assembly.GetType("UnityEditor.GameViewSizeType");
        var sizes = sizesType.BaseType.GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
        var constructor = sizeType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { kindType, typeof(int), typeof(int), typeof(string) }, null);
        int height = Array.IndexOf(Environment.GetCommandLineArgs(), "-qaTall") >= 0 ? 1600 : 1280;
        var size = constructor.Invoke(new object[] { Enum.ToObject(kindType, 1), 720, height, "Secret Four portrait validation" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        var gameViewType = assembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SetValue(gameView, count - 1);
    }
}

public class SecretFourFlowProbe : MonoBehaviour
{
    private readonly List<string> report = new List<string>();
    private float deadline;
    private void Awake() { DontDestroyOnLoad(gameObject); deadline = Time.realtimeSinceStartup + 120; }
    private void Update() { if (Time.realtimeSinceStartup > deadline) Finish("Timed out waiting for a game transition."); }
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private void Check(bool condition, string message)
    {
        if (!condition) { Finish(message); throw new Exception(message); }
    }
    private static int Count(Board board)
    {
        int count = 0;
        for (int x = 0; x < board.Width; x++)
        for (int y = 0; y < board.Height; y++) if (!board.IsEmpty(x, y)) count++;
        return count;
    }
    private IEnumerator Start()
    {
        yield return null; yield return null;
        MainMenuManager menu = Object.FindFirstObjectByType<MainMenuManager>();
        Check(menu != null && GameSession.Instance != null, "Main menu/session did not initialize.");
        Capture("menu");
        menu.OpenBotPanel(); yield return null;
        Check(GameObject.Find("BotSelectionPanel") != null, "Bot menu failed to open.");
        menu.BackFromBotPanel(); menu.OpenHumanSetup(); yield return null;
        Check(GameObject.Find("HumanSetupPanel") != null, "Local setup failed to open.");
        menu.BackToMainMenu(); menu.OpenHowToPlay(); yield return null;
        Check(GameObject.Find("RulesPanel") != null, "Rules failed to open.");
        Capture("rules");
        menu.CloseHowToPlay(); menu.OpenSettings(); yield return null;
        Check(GameObject.Find("SettingsPanel") != null, "Settings failed to open.");
        menu.CloseSettings(); menu.PlayInRoom(); yield return null;
        Check(GameObject.Find("PlayInRoomPanel") != null, "Room menu failed to open.");
        menu.BackFromPlayInRoom(); menu.OpenStore(); yield return null; yield return null;
        Check(GameObject.Find("StorePanel") != null, "Store failed to open.");
        menu.CloseStore();
        report.Add("PASS: main menu, bot picker, human setup, rules, settings, room menu, store, and Back navigation.");
        menu.StartHumanGame("Player 1", "Player 2");
        yield return null; yield return null; yield return null;
        var turn = Object.FindFirstObjectByType<TurnManager>();
        var board = Object.FindFirstObjectByType<BoardManager>().CurrentBoard;
        var placement = Object.FindFirstObjectByType<PlacementManager>();
        var selector = Object.FindFirstObjectByType<DirectionSelector>();
        var movement = Object.FindFirstObjectByType<MovementManager>();
        var result = Object.FindFirstObjectByType<GameResultManager>();
        var match = Object.FindFirstObjectByType<MatchManager>();
        var pause = Object.FindFirstObjectByType<PauseManager>();
        var view = Object.FindFirstObjectByType<BoardView>();
        Check(board.Width == 6 && board.Height == 7, "Expected configured portrait board.");
        Set(movement, "revealDelay", 0.02f); Set(movement, "movementDuration", 0.01f);
        Set(movement, "blockedPauseDuration", 0.01f); Set(movement, "returnDuration", 0.01f);
        Set(result, "resultDelay", 0.01f);
        Capture("game");
        var cellRenderer = Object.FindFirstObjectByType<CellView>().GetComponent<SpriteRenderer>();
        report.Add("Cell sprite: " + cellRenderer.sprite.name + " texture=" + cellRenderer.sprite.texture.name + " rect=" + cellRenderer.sprite.rect + " material=" + cellRenderer.sharedMaterial.name + " shader=" + cellRenderer.sharedMaterial.shader.name);
        Check(Object.FindObjectsByType<CellView>(FindObjectsSortMode.None).Length == 42, "Board did not create 42 cells.");
        for (int cx = 0; cx < board.Width; cx++)
        for (int cy = 0; cy < board.Height; cy++)
        {
            Vector3 point = Camera.main.WorldToScreenPoint(view.GetCellWorldPosition(cx, cy));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            foreach (var hit in hits) Check(!(hit.module is GraphicRaycaster), "UI is blocking cell " + cx + "," + cy + ": " + hit.gameObject.name);
        }
        pause.PauseGame();
        Check(pause.IsPaused() && Time.timeScale == 0, "Pause did not stop the game.");
        placement.TryPlaceCoin(2, 3);
        Check(Count(board) == 0, "Paused board accepted input.");
        pause.ResumeGame(); yield return null; yield return null; yield return null;
        Check(!pause.IsPaused() && Time.timeScale == 1, "Resume did not restore the game.");
        for (int round = 0; round < 4; round++)
        {
            placement.TryPlaceCoin(round, 0);
            Check(turn.CurrentState == GameState.WaitingForPlayerXDirection, "X placement failed.");
            selector.SelectStay();
            placement.TryPlaceCoin(round == 3 ? 5 : round * 2, round == 3 ? 5 : 6);
            Check(turn.CurrentState == GameState.WaitingForPlayerYDirection, "Y placement failed.");
            selector.SelectStay();
            while (movement.IsMovementInProgress) yield return null;
        }
        yield return new WaitForSeconds(0.05f);
        Check(result.HasResult && !result.IsDraw && result.Winner == PlayerType.X, "Local four-in-a-row did not finish with X winner.");
        Check(GameObject.Find("GameOverPanel") != null, "Result panel did not appear.");
        Object.FindFirstObjectByType<GameOverUI>().OnPlayAgainPressed(); yield return null;
        Check(Count(board) == 0 && !result.HasResult && turn.CurrentState == GameState.WaitingForPlayerXPlacement, "Play Again did not reset the match.");
        report.Add("PASS: board input, pause/resume, full local match, win presentation, and Play Again.");
        var bot = Object.FindFirstObjectByType<BotController>();
        Set(bot, "thinkingDelay", 0.01f);
        foreach (BotDifficulty difficulty in Enum.GetValues(typeof(BotDifficulty)))
        {
            GameSession.Instance.StartBotGame(difficulty); match.StartNewMatch(); yield return null;
            placement.TryPlaceCoin(2, 3); selector.SelectStay();
            placement.TryPlaceCoin(0, 0); selector.SelectStay();
            Check(Count(board) == 1 && turn.CurrentState == GameState.WaitingForPlayerYPlacement, "Human input stole the bot turn.");
            while (turn.CurrentState != GameState.WaitingForPlayerXPlacement) yield return null;
            Check(Count(board) == 2, difficulty + " bot turn did not complete.");
            report.Add("PASS: " + difficulty + " bot placement, direction, resolution, and protected turn.");
        }
        GameSession.Instance.StartLocalHumanGame("Player 1", "Player 2"); match.StartNewMatch();
        for (int round = 0; round < 4; round++)
        {
            placement.TryPlaceCoin(round, 1); selector.SelectStay();
            placement.TryPlaceCoin(round, 4); selector.SelectStay();
            while (movement.IsMovementInProgress) yield return null;
        }
        Check(result.IsDraw, "Simultaneous wins must draw.");
        report.Add("PASS: simultaneous four-in-a-row draw through the live match flow.");
        Object.FindFirstObjectByType<GameOverUI>().OnMainMenuPressed();
        yield return null; yield return null; yield return null;
        Check(SceneManager.GetActiveScene().name == "Mainmenu", "Return to menu failed.");
        report.Add("PASS: return to the main menu after a completed match.");
        Finish(null);
    }
    private void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Camera camera = Camera.main;
        if (camera == null) return;
        var canvas = Object.FindFirstObjectByType<Canvas>();
        int width = Screen.width, height = Screen.height;
        var target = new RenderTexture(width, height, 24);
        var oldTarget = camera.targetTexture;
        float oldAspect = camera.aspect;
        RenderMode mode = canvas.renderMode;
        Camera oldCamera = canvas.worldCamera;
        camera.targetTexture = target;
        camera.aspect = (float)width / height;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();
        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
            new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        Directory.CreateDirectory("ValidationResults");
        File.WriteAllBytes("ValidationResults/" + name + ".png", texture.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = oldTarget;
        camera.aspect = oldAspect;
        canvas.renderMode = mode;
        canvas.worldCamera = oldCamera;
        Destroy(texture); Destroy(target);
        Canvas.ForceUpdateCanvases();
    }

    private void Finish(string failure)
    {
        if (failure != null) report.Add("FAIL: " + failure);
        Directory.CreateDirectory("ValidationResults");
        File.WriteAllLines("ValidationResults/flow.txt", report);
        EditorApplication.Exit(failure == null ? 0 : 1);
        enabled = false;
    }
}
#endif
