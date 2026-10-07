#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SecretFourValidation
{
    private static int checks;
    private static readonly List<string> report = new List<string>();
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static PlayerAction Act(PlayerType p, int x, int y, MoveDirection d) =>
        new PlayerAction(p) { position = new Vector2Int(x, y), direction = d, hasPlaced = true, hasChosenDirection = true };
    private static int Count(Board board)
    {
        int count = 0;
        for (int x = 0; x < board.Width; x++)
        for (int y = 0; y < board.Height; y++) if (!board.IsEmpty(x, y)) count++;
        return count;
    }
    private static void Resolve(Board board, PlayerAction x, PlayerAction y)
    {
        int count = Count(board);
        SecretFourRules.Apply(board, SecretFourRules.Resolve(board, x, y));
        Check(Count(board) == count, "Movement lost or duplicated a coin.");
    }
    [MenuItem("Tools/Secret Four/Validate Rules and Bots")]
    public static void RunRules()
    {
        checks = 0;
        report.Clear();
        var randomState = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(73421);
            MovementCases();
            WinCases();
            TacticalCases();
            VisualTracking();
            SimulatedMatches();
            Check(PlayerNameManager.Normalize("    ") == "Player 1", "Blank name fallback");
            Check(System.Text.Encoding.UTF8.GetByteCount(PlayerNameManager.Normalize(new string('界', 80))) <= 60, "Network name byte limit");
            report.Add("PASS: " + checks + " assertions.");
            Debug.Log(string.Join("\n", report));
        }
        finally { UnityEngine.Random.state = randomState; }
    }
    private static void MovementCases()
    {
        var board = new Board(6, 7);
        board.TryPlace(1, 1, PlayerType.X); board.TryPlace(3, 1, PlayerType.Y);
        Resolve(board, Act(PlayerType.X, 1, 1, MoveDirection.Right), Act(PlayerType.Y, 3, 1, MoveDirection.Left));
        Check(board.GetCell(1, 1).Occupant == PlayerType.X && board.GetCell(3, 1).Occupant == PlayerType.Y, "Same destination must bounce both coins.");
        board.ClearBoard(); board.TryPlace(1, 1, PlayerType.X); board.TryPlace(2, 1, PlayerType.Y);
        Resolve(board, Act(PlayerType.X, 1, 1, MoveDirection.Right), Act(PlayerType.Y, 2, 1, MoveDirection.Left));
        Check(board.GetCell(2, 1).Occupant == PlayerType.X && board.GetCell(1, 1).Occupant == PlayerType.Y, "Legal swap.");
        board.ClearBoard(); board.TryPlace(1, 1, PlayerType.X); board.TryPlace(2, 1, PlayerType.Y);
        Resolve(board, Act(PlayerType.X, 1, 1, MoveDirection.Right), Act(PlayerType.Y, 2, 1, MoveDirection.Right));
        Check(board.GetCell(2, 1).Occupant == PlayerType.X && board.GetCell(3, 1).Occupant == PlayerType.Y, "Follow into vacated cell.");
        board.ClearBoard(); board.TryPlace(1, 1, PlayerType.X); board.TryPlace(2, 1, PlayerType.Y); board.TryPlace(3, 1, PlayerType.X);
        Resolve(board, Act(PlayerType.X, 1, 1, MoveDirection.Right), Act(PlayerType.Y, 2, 1, MoveDirection.Right));
        Check(board.GetCell(1, 1).Occupant == PlayerType.X && board.GetCell(2, 1).Occupant == PlayerType.Y, "Blocked following move.");
        board.ClearBoard(); board.TryPlace(0, 0, PlayerType.X); board.TryPlace(1, 0, PlayerType.Y);
        Resolve(board, Act(PlayerType.X, 0, 0, MoveDirection.Left), Act(PlayerType.Y, 1, 0, MoveDirection.Left));
        Check(board.GetCell(0, 0).Occupant == PlayerType.X && board.GetCell(1, 0).Occupant == PlayerType.Y, "Boundary and occupied destination.");
        Check(!SecretFourRules.CanChooseDirection(board, Vector2Int.zero, (MoveDirection)99), "Invalid direction rejected.");
        board = new Board(1, 1); board.TryPlace(0, 0, PlayerType.X);
        Resolve(board, Act(PlayerType.X, 0, 0, MoveDirection.Stay), new PlayerAction(PlayerType.Y));
        Check(board.IsFull(), "An unmatched final placement must resolve without losing its coin.");
        report.Add("PASS: collisions, swaps, following, blocked chains, boundaries, Stay, and final single placement.");
    }
    private static void WinCases()
    {
        foreach (var axis in new[] { Vector2Int.right, Vector2Int.up, new Vector2Int(1, 1), new Vector2Int(1, -1) })
        {
            var board = new Board(6, 7);
            for (int i = 0; i < 4; i++) board.TryPlace(1 + i * axis.x, 3 + i * axis.y, PlayerType.X);
            Check(SecretFourRules.HasLine(board, PlayerType.X), "Win axis " + axis);
        }
        var tie = new Board(6, 7);
        for (int i = 0; i < 4; i++) { tie.TryPlace(i, 1, PlayerType.X); tie.TryPlace(i, 2, PlayerType.Y); }
        Check(SecretFourRules.HasLine(tie, PlayerType.X) && SecretFourRules.HasLine(tie, PlayerType.Y), "Simultaneous wins.");
        var custom = new Board(3, 3, 3);
        for (int i = 0; i < 3; i++) custom.TryPlace(i, i, PlayerType.Y);
        Check(SecretFourRules.HasLine(custom, PlayerType.Y), "Configured win length.");
        report.Add("PASS: horizontal, vertical, both diagonals, simultaneous wins, configured win length.");
    }
    private static void TacticalCases()
    {
        foreach (var difficulty in new[] { BotDifficulty.Medium, BotDifficulty.Hard })
        {
            var board = new Board(6, 7);
            for (int i = 0; i < 3; i++) board.TryPlace(i, 0, PlayerType.Y);
            board.TryPlace(5, 6, PlayerType.X);
            string before = Fingerprint(board);
            var plan = BotPlanner.Choose(board, new Vector2Int(5, 6), difficulty);
            Check(plan.valid && board.IsEmpty(plan.position.x, plan.position.y), "Bot must choose an empty cell.");
            Check(Fingerprint(board) == before, "Bot search must not mutate the real board.");
            board.TryPlace(plan.position.x, plan.position.y, PlayerType.Y);
            Resolve(board, Act(PlayerType.X, 5, 6, MoveDirection.Stay), Act(PlayerType.Y, plan.position.x, plan.position.y, plan.direction));
            Check(SecretFourRules.HasLine(board, PlayerType.Y), difficulty + " must take a guaranteed win.");
        }
        var threat = new Board(6, 7);
        threat.TryPlace(0, 0, PlayerType.X); threat.TryPlace(1, 0, PlayerType.X); threat.TryPlace(2, 0, PlayerType.X);
        threat.TryPlace(3, 1, PlayerType.X);
        var response = BotPlanner.Choose(threat, new Vector2Int(3, 1), BotDifficulty.Hard);
        foreach (MoveDirection direction in SecretFourRules.LegalDirections(threat, new Vector2Int(3, 1)))
        {
            var test = threat.Copy(); test.TryPlace(response.position.x, response.position.y, PlayerType.Y);
            Resolve(test, Act(PlayerType.X, 3, 1, direction), Act(PlayerType.Y, response.position.x, response.position.y, response.direction));
            Check(!SecretFourRules.HasLine(test, PlayerType.X), "Hard must block a preventable immediate loss for every hidden direction.");
        }
        report.Add("PASS: tactical wins, robust blocking, hidden-direction search, and board immutability.");
    }
    private static string Fingerprint(Board board)
    {
        var text = new System.Text.StringBuilder();
        for (int x = 0; x < board.Width; x++)
        for (int y = 0; y < board.Height; y++) text.Append(board.GetCell(x, y).Occupant?.ToString() ?? ".");
        return text.ToString();
    }
    private static void VisualTracking()
    {
        var owner = new GameObject("Validation placement");
        var x = new GameObject("X"); var y = new GameObject("Y");
        try
        {
            var placement = owner.AddComponent<PlacementManager>();
            placement.UpdateCoinPosition(new Vector2Int(0, 0), new Vector2Int(0, 0), x);
            placement.UpdateCoinPosition(new Vector2Int(1, 0), new Vector2Int(1, 0), y);
            var resolution = new MovementResolution(
                new MovementResult(PlayerType.X, new Vector2Int(0, 0), new Vector2Int(1, 0), MoveDirection.Right),
                new MovementResult(PlayerType.Y, new Vector2Int(1, 0), new Vector2Int(0, 0), MoveDirection.Left));
            placement.UpdateCoinPositions(resolution, x, y);
            Check(placement.GetCoinAtPosition(new Vector2Int(1, 0)) == x && placement.GetCoinAtPosition(new Vector2Int(0, 0)) == y,
                "Both visual references must survive a swap.");
            Check(placement.GetAllCoins().Count() == 2, "Visual dictionary size after swap.");
        }
        finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(x); Object.DestroyImmediate(y); }
        report.Add("PASS: simultaneous visual tracking.");
    }
    private static void SimulatedMatches()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        double slowest = 0;
        foreach (BotDifficulty difficulty in Enum.GetValues(typeof(BotDifficulty)))
        for (int match = 0; match < 20; match++)
        {
            var board = new Board(6, 7);
            for (int round = 0; round < 21; round++)
            {
                var empty = new List<Vector2Int>();
                for (int x = 0; x < board.Width; x++)
                for (int y = 0; y < board.Height; y++) if (board.IsEmpty(x, y)) empty.Add(new Vector2Int(x, y));
                if (empty.Count == 0) break;
                Vector2Int xp = empty[UnityEngine.Random.Range(0, empty.Count)];
                board.TryPlace(xp.x, xp.y, PlayerType.X);
                var dirs = SecretFourRules.LegalDirections(board, xp);
                var xa = Act(PlayerType.X, xp.x, xp.y, dirs[UnityEngine.Random.Range(0, dirs.Count)]);
                double start = watch.Elapsed.TotalMilliseconds;
                var plan = BotPlanner.Choose(board, xp, difficulty);
                slowest = Math.Max(slowest, watch.Elapsed.TotalMilliseconds - start);
                Check(plan.valid && board.IsEmpty(plan.position.x, plan.position.y), "Legal bot placement in full match.");
                Check(SecretFourRules.CanChooseDirection(board, plan.position, plan.direction, xp), "Legal bot direction in full match.");
                board.TryPlace(plan.position.x, plan.position.y, PlayerType.Y);
                Resolve(board, xa, Act(PlayerType.Y, plan.position.x, plan.position.y, plan.direction));
                Check(Count(board) == (round + 1) * 2, "Every complete round adds exactly two coins.");
                if (SecretFourRules.HasLine(board, PlayerType.X) || SecretFourRules.HasLine(board, PlayerType.Y) || board.IsFull()) break;
            }
        }
        report.Add("PASS: 60 seeded full matches across all difficulties. Slowest decision: " + slowest.ToString("F1") + " ms.");
    }
    public static void RunAllChecks()
    {
        RunRules();
        foreach (string scenePath in new[] { "Assets/Scenes/Mainmenu.unity", "Assets/Scenes/Game.unity" }) AuditScene(scenePath);
        Directory.CreateDirectory("ValidationResults");
        File.WriteAllLines("ValidationResults/rules.txt", report);
    }
    public static void RunBatch()
    {
        try
        {
            RunAllChecks();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory("ValidationResults");
            File.WriteAllText("ValidationResults/rules.txt", "FAIL: " + exception);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
    private static void AuditScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath);
        int buttons = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + transform.name);
            foreach (MonoBehaviour component in transform.GetComponents<MonoBehaviour>())
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                            "Broken reference: " + transform.name + "." + property.propertyPath);
            }
            foreach (Button button in transform.GetComponents<Button>())
            {
                buttons++;
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    Object target = button.onClick.GetPersistentTarget(i);
                    string method = button.onClick.GetPersistentMethodName(i);
                    Check(target != null && target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Any(m => m.Name == method),
                        "Disconnected button: " + transform.name + " -> " + method);
                }
            }
        }
        report.Add("PASS: " + scenePath + ", " + buttons + " buttons; no missing scripts, broken references, or missing button methods.");
    }
}
#endif
