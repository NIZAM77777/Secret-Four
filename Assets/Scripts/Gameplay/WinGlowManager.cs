using System.Collections.Generic;
using UnityEngine;

public class WinGlowManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlacementManager placementManager;

    [SerializeField]
    private WinConditionChecker winConditionChecker;


    // =========================================
    // GLOW WINNING COINS
    // =========================================

    public void GlowWinningCoins()
    {
        if (placementManager == null ||
            winConditionChecker == null)
        {
            Debug.LogError(
                "WinGlowManager: Missing references."
            );

            return;
        }


        List<Vector2Int> positions =
            winConditionChecker.WinningPositions;


        foreach (Vector2Int position
            in positions)
        {
            GameObject coin =
                placementManager.GetCoinAtPosition(
                    position
                );


            if (coin == null)
            {
                Debug.LogWarning(
                    $"Winning coin not found: {position}"
                );

                continue;
            }


            WinningCoinGlow glow =
                coin.GetComponent<WinningCoinGlow>();


            if (glow != null)
            {
                glow.StartGlow();
            }
        }
    }


    // =========================================
    // STOP ALL GLOWS
    // =========================================

    public void StopAllGlows()
    {
        if (placementManager == null)
        {
            return;
        }


        foreach (
            GameObject coin
            in placementManager.GetAllCoins()
        )
        {
            if (coin == null)
            {
                continue;
            }


            WinningCoinGlow glow =
                coin.GetComponent<WinningCoinGlow>();


            if (glow != null)
            {
                glow.StopGlow();
            }
        }
    }
}