using UnityEngine;
using Unity.Netcode;

public class NetworkGameResultManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private WinConditionChecker winConditionChecker;
    [SerializeField] private GameResultManager gameResultManager;
    [SerializeField] private TurnManager turnManager;



    public bool CheckGameResultOnHost()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only Host can check the online game result.");
            return false;
        }

        if (winConditionChecker == null || gameResultManager == null)
        {
            Debug.LogError("Network game-result references are missing.");
            return false;
        }

        // A result has already been accepted. Do not send another result RPC.
        if (gameResultManager.HasResult)
            return true;

        if (winConditionChecker.HasWinner(out PlayerType winner))
        {
            Debug.Log($"HOST: Winner is {winner}");

            gameResultManager.SetWinner(winner);

            NetworkGameState.Instance?.SetWinner(winner);
            NetworkGameState.Instance?.CaptureFromServer();

            return true;
        }

        if (winConditionChecker.HasSimultaneousWin || winConditionChecker.IsBoardFull())
        {
            Debug.Log("HOST: Game is a draw.");

            gameResultManager.SetDraw();
            NetworkGameState.Instance?.SetDraw();
            NetworkGameState.Instance?.CaptureFromServer();
            return true;
        }

        NetworkGameState.Instance?.CaptureFromServer();
        return false;
    }





    public void ResolveAfterAuthoritativeMovement()
    {
        if (!IsServer)
            return;

        if (NetworkGameState.Instance != null)
            NetworkGameState.Instance.SetAuthoritativePhase(GameState.CheckingResult);

        bool resultFound = CheckGameResultOnHost();
        if (resultFound)
            return;

        StartNextRoundOnHost();
    }

    public void StartNextRoundOnHost()
    {
        if (!IsServer)
            return;

        if (gameResultManager != null && gameResultManager.HasResult)
        {
            Debug.LogWarning("Cannot start the next round after a completed result.");
            return;
        }

        // Reset the local turn/action cache first.
        turnManager?.StartNextRound();

        // IMPORTANT:
        // CaptureFromServer intentionally preserves an authoritative
        // presentation phase such as CheckingResult. Therefore merely
        // changing TurnManager.CurrentState is not enough to advance the
        // network match into Round 2.
        //
        // Commit the new round as a complete authoritative snapshot so both
        // clients receive:
        //   - WaitingForPlayerXPlacement
        //   - cleared X/Y round actions
        //   - cleared placement/direction flags
        // in the same network revision.
        NetworkGameState state = NetworkGameState.Instance;
        if (state != null)
        {
            state.CommitAuthoritativePhase(
                GameState.WaitingForPlayerXPlacement,
                turnManager.PlayerXAction,
                turnManager.PlayerYAction);
        }

        Debug.Log("===== AUTHORITATIVE ONLINE NEXT ROUND STARTED =====");
    }
}
