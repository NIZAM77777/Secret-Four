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

    private readonly Dictionary<Vector2Int, GameObject> coinVisuals = new();

    private void Start()
    {
        if (coinParent == null)
            coinParent = transform;
    }

    public void TryPlaceCoin(int x, int y)
    {
        if (IsOnlineGame())
        {
            if (networkCoinPlacementManager == null)
            {
                Debug.LogError("NetworkCoinPlacementManager is missing.");
                return;
            }

            PlayerType player = GetCurrentPlacementPlayer();
            if (!IsLocalPlayerAllowed(player))
            {
                Debug.Log("It is not your turn.");
                return;
            }

            networkCoinPlacementManager.RequestPlaceCoin(x, y);
            return;
        }

        ApplyCoinPlacement(x, y, GetCurrentPlacementPlayer());
    }

    /// <summary>
    /// Applies an accepted placement. When a network skin ID is supplied, that exact
    /// player's selected skin is used instead of reading the local player's selection.
    /// </summary>
    public bool ApplyCoinPlacement(int x, int y, PlayerType player, string selectedSkinId = null)
    {
        Debug.Log($"PLACE ATTEMPT ({x},{y}) | Player = {player} | State = {turnManager.CurrentState}");

        if (!turnManager.CanPlaceCoin(player))
        {
            Debug.Log("Coin cannot be placed right now.");
            return false;
        }

        Board board = boardManager.CurrentBoard;
        if (!board.IsInsideBoard(x, y) || !board.IsEmpty(x, y))
            return false;

        if (!board.TryPlace(x, y, player))
            return false;

        CreateCoinVisual(x, y, player, selectedSkinId);

        PlayerAction action = turnManager.GetAction(player);
        action.position = new Vector2Int(x, y);
        action.hasPlaced = true;

        Debug.Log($"{player} placed at ({x}, {y})");
        turnManager.PlayerPlaced(player);
        directionButtonController.UpdateDirectionButtons(player);
        return true;
    }

    private void CreateCoinVisual(int x, int y, PlayerType playerType, string selectedSkinId = null)
    {
        Vector3 position = boardView.GetCellWorldPosition(x, y);
        GameObject coin = Instantiate(coinPrefab, position, Quaternion.identity, coinParent);
        coin.name = $"{playerType}_Coin_{x}_{y}";

        Vector2Int boardPosition = new(x, y);
        coinVisuals[boardPosition] = coin;

        CoinView coinView = coin.GetComponent<CoinView>();
        if (coinView != null)
        {
            Sprite sprite = null;

            if (!string.IsNullOrWhiteSpace(selectedSkinId))
                sprite = SecretFourStoreIntegration.GetSpriteForItem(selectedSkinId);

            if (sprite == null)
                sprite = SecretFourStoreIntegration.GetSelectedPlayerSprite();

            if (sprite == null && coinConfig != null)
                sprite = coinConfig.GetSprite(playerType);

            coinView.Setup(playerType, sprite);
        }

        if (playerType == PlayerType.X)
            PlayerXCoin = coin;
        else
            PlayerYCoin = coin;
    }

    public GameObject GetCoinAtPosition(Vector2Int position)
    {
        return coinVisuals.TryGetValue(position, out GameObject coin) ? coin : null;
    }

    public void ClearAllCoins()
    {
        if (coinParent == null)
            return;

        for (int i = coinParent.childCount - 1; i >= 0; i--)
            Destroy(coinParent.GetChild(i).gameObject);

        coinVisuals.Clear();
        PlayerXCoin = null;
        PlayerYCoin = null;
    }

    private bool IsOnlineGame()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private bool IsLocalPlayerAllowed(PlayerType player)
    {
        if (NetworkManager.Singleton == null)
            return true;

        if (NetworkManager.Singleton.IsHost && player == PlayerType.X)
            return true;

        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost && player == PlayerType.Y)
            return true;

        return false;
    }

    private PlayerType GetCurrentPlacementPlayer()
    {
        return turnManager.CurrentState == GameState.WaitingForPlayerXPlacement
            ? PlayerType.X
            : PlayerType.Y;
    }

    public void UpdateCoinPosition(Vector2Int oldPosition, Vector2Int newPosition, GameObject coin)
    {
        if (coinVisuals.ContainsKey(oldPosition))
            coinVisuals.Remove(oldPosition);

        coinVisuals[newPosition] = coin;
    }

    public void UpdateCoinPositions(MovementResolution resolution, GameObject xCoin, GameObject yCoin)
    {
        Vector2Int xOld = resolution.playerXResult.startPosition;
        Vector2Int xNew = resolution.playerXResult.destination;
        if (xOld != xNew && xCoin != null)
            UpdateCoinPosition(xOld, xNew, xCoin);

        Vector2Int yOld = resolution.playerYResult.startPosition;
        Vector2Int yNew = resolution.playerYResult.destination;
        if (yOld != yNew && yCoin != null)
            UpdateCoinPosition(yOld, yNew, yCoin);
    }

    public IEnumerable<GameObject> GetAllCoins()
    {
        return coinVisuals.Values;
    }
}
