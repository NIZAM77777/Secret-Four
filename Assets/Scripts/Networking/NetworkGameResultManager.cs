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

            if (winGlowManager != null)
                winGlowManager.GlowWinningCoins();

            SendWinnerClientRpc((int)winner);
            return true;
        }

        if (winConditionChecker.IsBoardFull())
        {
            Debug.Log("HOST: Game is a draw.");
            SendDrawClientRpc();
            return true;
        }

        return false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendWinnerClientRpc(int winnerValue)
    {
        if (!IsValidPlayerType(winnerValue))
        {
            Debug.LogWarning($"NETWORK RESULT: invalid winner value {winnerValue}");
            return;
        }

        PlayerType winner = (PlayerType)winnerValue;

        Debug.Log($"NETWORK RESULT: {winner} WINS");

        if (winGlowManager != null)
            winGlowManager.GlowWinningCoins();

        if (gameResultManager != null)
            gameResultManager.SetWinner(winner);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendDrawClientRpc()
    {
        Debug.Log("NETWORK RESULT: DRAW");

        if (gameResultManager != null)
            gameResultManager.SetDraw();
    }

    public void StartNextRoundOnHost()
    {
        if (!IsServer)
            return;

        if (gameResultManager != null && !gameResultManager.HasResult)
        {
            Debug.LogWarning("Cannot start the next round before the match has a result.");
            return;
        }

        StartNextRoundClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StartNextRoundClientRpc()
    {
        if (turnManager == null)
            return;

        Debug.Log("NETWORK: START NEXT ROUND");
        turnManager.StartNextRound();
    }

    private bool IsValidPlayerType(int value)
    {
        return value == (int)PlayerType.X || value == (int)PlayerType.Y;
    }
}
