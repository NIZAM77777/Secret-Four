using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlacementManager : MonoBehaviour
{
    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private BoardView boardView;

    [SerializeField]
    private CoinConfig coinConfig;

    [SerializeField]
    private DirectionButtonController directionButtonController;

    [SerializeField]
    private GameObject coinPrefab;

    [SerializeField]
    private Transform coinParent;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private NetworkCoinPlacementManager networkCoinPlacementManager;


    public GameObject PlayerXCoin { get; private set; }

    public GameObject PlayerYCoin { get; private set; }


    // =========================================
    // ALL COIN VISUALS
    // Position → Coin GameObject
    // =========================================

    private Dictionary<Vector2Int, GameObject>
        coinVisuals =
        new Dictionary<Vector2Int, GameObject>();


    private void Start()
    {
        if (coinParent == null)
        {
            coinParent = transform;
        }
    }


    // =========================================
    // MAIN PLACEMENT REQUEST
    // =========================================

    public void TryPlaceCoin(int x, int y)
    {
        // ---------------------------------
        // ONLINE GAME
        // ---------------------------------

        if (IsOnlineGame())
        {
            if (networkCoinPlacementManager == null)
            {
                Debug.LogError(
                    "NetworkCoinPlacementManager is missing."
                );

                return;
            }


            PlayerType player =
                GetCurrentPlacementPlayer();


            if (!IsLocalPlayerAllowed(player))
            {
                Debug.Log(
                    "It is not your turn."
                );

                return;
            }


            networkCoinPlacementManager
                .RequestPlaceCoin(x, y);

            return;
        }


        // ---------------------------------
        // LOCAL GAME
        // ---------------------------------

        PlayerType localPlayer =
            GetCurrentPlacementPlayer();


        ApplyCoinPlacement(
            x,
            y,
            localPlayer
        );
    }


    // =========================================
    // APPLY COIN PLACEMENT
    // =========================================

    public bool ApplyCoinPlacement(
        int x,
        int y,
        PlayerType player)
    {
        Debug.Log(
            $"PLACE ATTEMPT ({x},{y}) | " +
            $"Player = {player} | " +
            $"State = {turnManager.CurrentState}"
        );


        if (!turnManager.CanPlaceCoin(player))
        {
            Debug.Log(
                "Coin cannot be placed right now."
            );

            return false;
        }


        Board board =
            boardManager.CurrentBoard;


        if (!board.IsInsideBoard(x, y))
        {
            return false;
        }


        if (!board.IsEmpty(x, y))
        {
            Debug.Log(
                $"CELL REJECTED ({x},{y}) — " +
                $"already occupied."
            );

            return false;
        }


        bool placed =
            board.TryPlace(
                x,
                y,
                player
            );


        if (!placed)
        {
            return false;
        }


        CreateCoinVisual(
            x,
            y,
            player
        );


        PlayerAction action =
            turnManager.GetAction(player);


        action.position =
            new Vector2Int(x, y);


        action.hasPlaced = true;


        Debug.Log(
            $"{player} placed at ({x}, {y})"
        );


        turnManager.PlayerPlaced(player);


        directionButtonController.UpdateDirectionButtons(
            player
        );


        return true;
    }


    // =========================================
    // CREATE VISUAL
    // =========================================

    private void CreateCoinVisual(
        int x,
        int y,
        PlayerType playerType)
    {
        Vector3 position =
            boardView.GetCellWorldPosition(
                x,
                y
            );


        GameObject coin =
            Instantiate(
                coinPrefab,
                position,
                Quaternion.identity,
                coinParent
            );


        coin.name =
            $"{playerType}_Coin_{x}_{y}";


        // =====================================
        // STORE COIN POSITION
        // =====================================

        Vector2Int boardPosition =
            new Vector2Int(x, y);


        coinVisuals[boardPosition] =
            coin;


        CoinView coinView =
            coin.GetComponent<CoinView>();


        if (coinView != null)
        {
            Sprite sprite =
                coinConfig.GetSprite(playerType);

            coinView.Setup(
                playerType,
                sprite
            );
        }


        if (playerType == PlayerType.X)
        {
            PlayerXCoin = coin;
        }
        else
        {
            PlayerYCoin = coin;
        }
    }


    // =========================================
    // GET COIN AT BOARD POSITION
    // =========================================

    public GameObject GetCoinAtPosition(
        Vector2Int position)
    {
        if (
            coinVisuals.TryGetValue(
                position,
                out GameObject coin
            )
        )
        {
            return coin;
        }


        return null;
    }


    // =========================================
    // CLEAR COINS
    // =========================================

    public void ClearAllCoins()
    {
        if (coinParent == null)
            return;


        for (
            int i = coinParent.childCount - 1;
            i >= 0;
            i--
        )
        {
            Destroy(
                coinParent.GetChild(i).gameObject
            );
        }


        // Clear stored coin references

        coinVisuals.Clear();


        PlayerXCoin = null;
        PlayerYCoin = null;
    }


    // =========================================
    // ONLINE CHECK
    // =========================================

    private bool IsOnlineGame()
    {
        if (NetworkManager.Singleton == null)
        {
            return false;
        }


        return NetworkManager.Singleton.IsListening;
    }


    // =========================================
    // LOCAL PLAYER PERMISSION
    // =========================================

    private bool IsLocalPlayerAllowed(
        PlayerType player)
    {
        if (NetworkManager.Singleton == null)
        {
            return true;
        }


        if (
            NetworkManager.Singleton.IsHost &&
            player == PlayerType.X
        )
        {
            return true;
        }


        if (
            NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost &&
            player == PlayerType.Y
        )
        {
            return true;
        }


        return false;
    }


    // =========================================
    // CURRENT PLAYER
    // =========================================

    private PlayerType GetCurrentPlacementPlayer()
    {
        if (
            turnManager.CurrentState ==
            GameState.WaitingForPlayerXPlacement
        )
        {
            return PlayerType.X;
        }


        return PlayerType.Y;
    }

    // =========================================
    // UPDATE COIN POSITION
    // =========================================

    // =========================================
    // UPDATE COIN POSITION
    // =========================================

    public void UpdateCoinPosition(
        Vector2Int oldPosition,
        Vector2Int newPosition,
        GameObject coin)
    {
        // Remove old position

        if (coinVisuals.ContainsKey(oldPosition))
        {
            coinVisuals.Remove(
                oldPosition
            );
        }


        // Store new position

        coinVisuals[newPosition] =
            coin;
    }

    // =========================================
    // UPDATE MOVED COINS
    // =========================================

    public void UpdateCoinPositions(
        MovementResolution resolution,
        GameObject xCoin,
        GameObject yCoin)
    {
        // =====================================
        // PLAYER X
        // =====================================

        Vector2Int xOld =
            resolution.playerXResult.startPosition;


        Vector2Int xNew =
            resolution.playerXResult.destination;


        if (xOld != xNew && xCoin != null)
        {
            UpdateCoinPosition(
                xOld,
                xNew,
                xCoin
            );
        }


        // =====================================
        // PLAYER Y
        // =====================================

        Vector2Int yOld =
            resolution.playerYResult.startPosition;


        Vector2Int yNew =
            resolution.playerYResult.destination;


        if (yOld != yNew && yCoin != null)
        {
            UpdateCoinPosition(
                yOld,
                yNew,
                yCoin
            );
        }
    }

    // =========================================
    // GET ALL COINS
    // =========================================

    public IEnumerable<GameObject> GetAllCoins()
    {
        return coinVisuals.Values;
    }

}