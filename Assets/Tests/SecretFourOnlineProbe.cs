#if SECRET_FOUR_VALIDATION
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

// Compiled only into the dedicated validation build, never the shipping game.
public class SecretFourOnlineProbe : MonoBehaviour
{
    private bool host;
    private string output;
    private float deadline;
    private bool finished;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-secretFourQA");
        if (index < 0) return;
        var probe = new GameObject("Online validation").AddComponent<SecretFourOnlineProbe>();
        probe.host = args[index + 1] == "host";
        probe.output = args[index + 2];
        probe.deadline = Time.realtimeSinceStartup + 90;
        DontDestroyOnLoad(probe.gameObject);
    }
    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup > deadline)
            Finish("FAIL: online transition timed out; scene=" + SceneManager.GetActiveScene().name +
                " phase=" + (NetworkGameState.Instance != null ? NetworkGameState.Instance.GetAuthoritativeState().ToString() : "none"));
    }
    private void Check(bool condition, string message)
    {
        if (condition) return;
        Finish("FAIL: " + message);
        throw new Exception(message);
    }
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private void Log(string message) => File.AppendAllText(Path.Combine(output, host ? "host.txt" : "client.txt"), message + Environment.NewLine);
    private IEnumerator Start()
    {
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, host ? "host.txt" : "client.txt"), "");
        yield return null; yield return null;
        NetworkManager network = NetworkManager.Singleton;
        Check(network != null, "NetworkManager missing from menu.");
        network.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 17943, "127.0.0.1");
        GameSession.Instance.StartOnlineRoomGame();
        if (host)
        {
            Check(network.StartHost(), "Host failed to start.");
            File.WriteAllText(Path.Combine(output, "host.ready"), "ready");
        }
        else
        {
            while (!File.Exists(Path.Combine(output, "host.ready"))) yield return null;
            Check(network.StartClient(), "Client failed to start.");
        }
        while (OnlineMatchPlayerState.Instance == null || !OnlineMatchPlayerState.Instance.IsMatchReady() ||
               NetworkGameState.Instance == null || !NetworkGameState.Instance.IsSpawned) yield return null;
        Log("PASS: connected, synchronized Game scene, and assigned both player profiles.");
        var movement = FindFirstObjectByType<MovementManager>();
        var board = FindFirstObjectByType<BoardManager>().CurrentBoard;
        var placement = FindFirstObjectByType<PlacementManager>();
        var selector = FindFirstObjectByType<DirectionSelector>();
        var result = FindFirstObjectByType<GameResultManager>();
        var ui = FindFirstObjectByType<GameOverUI>();
        var state = NetworkGameState.Instance;
        Set(movement, "revealDelay", 0.12f); Set(movement, "movementDuration", 0.08f);
        Set(FindFirstObjectByType<NetworkMovementManager>(), "revealDelay", 0.12f);
        Set(result, "resultDelay", 0.1f);
        FindFirstObjectByType<PauseManager>().PauseGame();
        Check(Time.timeScale == 1, "Online pause stopped the match.");
        for (int match = 0; match < 2; match++)
        {
            int matchId = state.MatchId.Value;
            for (int round = 0; round < 4; round++)
            {
                PlayerType side = host ? PlayerType.X : PlayerType.Y;
                while (!state.CanRequestPlacement(side) || movement.IsMovementInProgress) yield return null;
                if (!host) Check(state.XDirection.Value == (int)MoveDirection.Stay, "Opponent secret direction leaked before reveal.");
                placement.TryPlaceCoin(host ? 0 : 5, host ? round : 6 - round * 2);
                while (!state.CanRequestDirection(side)) yield return null;
                if (host) selector.SelectRight(); else selector.SelectLeft();
                while (state.MoveId.Value < round + 1 || movement.IsMovementInProgress || state.IsPresentationPhase ||
                       board.GetCell(1, round).Occupant != PlayerType.X ||
                       board.GetCell(4, 6 - round * 2).Occupant != PlayerType.Y) yield return null;
                // The client applies an authoritative snapshot on the following frame.
                yield return null; yield return null;
                Check(board.GetCell(1, round).Occupant == PlayerType.X, "X movement lost in round " + round);
                Check(board.GetCell(4, 6 - round * 2).Occupant == PlayerType.Y, "Y movement lost in round " + round);
                Log("PASS: match " + (match + 1) + ", round " + (round + 1) + " synchronized positions and turn.");
            }
            while (!result.HasResult || GameObject.Find("GameOverPanel") == null) yield return null;
            Check(!result.IsDraw && result.Winner == PlayerType.X, "Winner disagreed after four rounds.");
            Check(state.MoveId.Value == 4, "Expected exactly four network resolutions.");
            Log("PASS: matching X result and result panel.");
            if (match == 0)
            {
                if (!host) yield return new WaitForSeconds(0.3f);
                ui.OnPlayAgainPressed();
                while (state.MatchId.Value == matchId) yield return null;
                yield return null; yield return null;
                Check(!result.HasResult && board.IsEmpty(1, 0) && state.MoveId.Value == 0, "Rematch did not clear the previous match.");
                Log("PASS: two-player rematch reset.");
            }
        }
        if (host)
        {
            while (GameObject.Find("DisconnectPanel") == null) yield return null;
            Log("PASS: opponent departure displayed.");
            FindFirstObjectByType<DisconnectUIManager>().ReturnToMainMenu();
        }
        else ui.OnMainMenuPressed();
        while (SceneManager.GetActiveScene().name != "Mainmenu") yield return null;
        yield return null;
        Check(FindObjectsByType<OnlineSceneLoader>(FindObjectsSortMode.None).Length == 1, "Duplicate room scene loaders remained after leaving.");
        Log("PASS: return to menu after network shutdown.");
        Finish("PASS: online flow complete.");
    }
    private void Finish(string message)
    {
        if (finished) return;
        finished = true;
        Log(message);
        Debug.Log(message);
        Application.Quit(message.StartsWith("PASS") ? 0 : 1);
    }
}
#endif
