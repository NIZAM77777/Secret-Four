using UnityEngine;
using Unity.Netcode;

public class NetworkGameResultManager : NetworkBehaviour
{
    [Header("References")]

    [SerializeField]
    private WinConditionChecker winConditionChecker;

    [SerializeField]
    private GameResultManager gameResultManager;

    [SerializeField]
    private TurnManager turnManager;


    // =====================================================
    // HOST CHECKS GAME RESULT
    // =====================================================

    public bool CheckGameResultOnHost()
    {
        if (!IsServer)
        {
            Debug.LogWarning(
                "Only Host can check the online game result."
            );

            return false;
        }

        if (
            winConditionChecker.HasWinner(
                out PlayerType winner
            )
        )
        {
            Debug.Log(
                $"HOST: Winner is {winner}"
            );

            SendWinnerClientRpc(
                (int)winner
            );

            return true;
        }

        if (winConditionChecker.IsBoardFull())
        {
            Debug.Log(
                "HOST: Game is a draw."
            );

            SendDrawClientRpc();

            return true;
        }

        return false;
    }


    // =====================================================
    // SEND WINNER
    // =====================================================

    [ClientRpc]
    private void SendWinnerClientRpc(
        int winnerValue)
    {
        PlayerType winner =
            (PlayerType)winnerValue;

        Debug.Log(
            $"NETWORK RESULT: {winner} WINS"
        );

        if (gameResultManager != null)
        {
            gameResultManager.SetWinner(
                winner
            );
        }
    }


    // =====================================================
    // SEND DRAW
    // =====================================================

    [ClientRpc]
    private void SendDrawClientRpc()
    {
        Debug.Log(
            "NETWORK RESULT: DRAW"
        );

        if (gameResultManager != null)
        {
            gameResultManager.SetDraw();
        }
    }


    // =====================================================
    // START NEXT ROUND
    // =====================================================

    public void StartNextRoundOnHost()
    {
        if (!IsServer)
        {
            return;
        }

        StartNextRoundClientRpc();
    }


    [ClientRpc]
    private void StartNextRoundClientRpc()
    {
        if (turnManager != null)
        {
            Debug.Log(
                "NETWORK: START NEXT ROUND"
            );

            turnManager.StartNextRound();
        }
    }
}