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


            // Check whether this local player
            // is allowed to place this coin

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
            Debug.Log(
                "Position is outside the board."
            );

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


        // Host controls Player X

        if (NetworkManager.Singleton.IsHost &&
            player == PlayerType.X)
        {
            return true;
        }


        // Client controls Player Y

        if (NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost &&
            player == PlayerType.Y)
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
        if (turnManager.CurrentState ==
            GameState.WaitingForPlayerXPlacement)
        {
            return PlayerType.X;
        }


        return PlayerType.Y;
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
    // CLEAR COINS
    // =========================================

    public void ClearAllCoins()
    {
        if (coinParent == null)
            return;


        for (int i = coinParent.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                coinParent.GetChild(i).gameObject
            );
        }


        PlayerXCoin = null;
        PlayerYCoin = null;
    }
}