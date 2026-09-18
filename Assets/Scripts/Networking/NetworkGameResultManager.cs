using UnityEngine;
using Unity.Netcode;

public class NetworkGameResultManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private WinConditionChecker winConditionChecker;
    [SerializeField] private GameResultManager gameResultManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private WinGlowManager winGlowManager;

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

            if (winGlowManager != null)
                winGlowManager.GlowWinningCoins();

            NetworkGameState.Instance?.SetWinner(winner);
            NetworkGameState.Instance?.CaptureFromServer();
            return true;
        }

        if (winConditionChecker.IsBoardFull())
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

    public void StartNextRoundOnHost()
    {
        if (!IsServer)
            return;

        if (gameResultManager != null && gameResultManager.HasResult)
        {
            Debug.LogWarning("Cannot start the next round after a completed result.");
            return;
        }

        // NetworkMovementManager advances the round only after all clients
        // have finished presenting the authoritative movement result.
        turnManager?.StartNextRound();
        NetworkGameState.Instance?.CaptureFromServer();
    }


}
