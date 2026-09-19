using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Authoritative online match state for Secret Four.
/// The server owns the state. Clients treat their Board/TurnManager copies as
/// synchronized presentation caches rather than independent authorities.
/// </summary>
public sealed class NetworkGameState : NetworkBehaviour
{
    private const byte EmptyCell = 0;
    private const byte PlayerXCell = 1;
    private const byte PlayerYCell = 2;

    private const int NoWinner = -1;
    private const int WinnerX = 0;
    private const int WinnerY = 1;
    private const int DrawResult = 2;

    public static NetworkGameState Instance { get; private set; }

    private NetworkVariable<int> currentState =
        new NetworkVariable<int>(
            (int)GameState.WaitingForPlayerXPlacement,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public GameState CurrentAuthoritativeState =>
        (GameState)currentState.Value;

    public NetworkVariable<int> WinnerState { get; } =
        new NetworkVariable<int>(
            NoWinner,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<int> StateRevision { get; } =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<int> MoveId { get; } =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<int> MatchId { get; } =
        new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<int> XPositionX { get; } =
        new NetworkVariable<int>(-1);
    public NetworkVariable<int> XPositionY { get; } =
        new NetworkVariable<int>(-1);
    public NetworkVariable<int> YPositionX { get; } =
        new NetworkVariable<int>(-1);
    public NetworkVariable<int> YPositionY { get; } =
        new NetworkVariable<int>(-1);

    public NetworkVariable<int> XDirection { get; } =
        new NetworkVariable<int>((int)MoveDirection.Up);
    public NetworkVariable<int> YDirection { get; } =
        new NetworkVariable<int>((int)MoveDirection.Up);

    public NetworkVariable<bool> XHasPlaced { get; } =
        new NetworkVariable<bool>(false);
    public NetworkVariable<bool> YHasPlaced { get; } =
        new NetworkVariable<bool>(false);
    public NetworkVariable<bool> XHasChosenDirection { get; } =
        new NetworkVariable<bool>(false);
    public NetworkVariable<bool> YHasChosenDirection { get; } =
        new NetworkVariable<bool>(false);

    public NetworkVariable<byte> RematchReadyMask { get; } =
        new NetworkVariable<byte>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkList<byte> BoardCells { get; private set; } =
        new NetworkList<byte>();

    public NetworkList<FixedString64Bytes> BoardSkinIds { get; private set; } =
        new NetworkList<FixedString64Bytes>();

    public bool IsPresentationPhase =>
        IsPhase(GameState.RevealingDirections) ||
        IsPhase(GameState.Moving) ||
        IsPhase(GameState.CheckingResult);

    private BoardManager boardManager;
    private TurnManager turnManager;
    private PlacementManager placementManager;
    private GameResultManager gameResultManager;
    private DirectionButtonController directionButtonController;

    private int lastAppliedRevision = -1;
    private bool applyScheduled;
    private bool serverInitialized;

    private void Awake()
    {
        Instance = this;

        boardManager = FindFirstObjectByType<BoardManager>();
        turnManager = FindFirstObjectByType<TurnManager>();
        placementManager = FindFirstObjectByType<PlacementManager>();
        gameResultManager = FindFirstObjectByType<GameResultManager>();
        directionButtonController = FindFirstObjectByType<DirectionButtonController>();
    }

    public override void OnNetworkSpawn()
    {
        currentState.OnValueChanged += OnNetworkValueChanged;
        WinnerState.OnValueChanged += OnNetworkValueChanged;
        StateRevision.OnValueChanged += OnNetworkValueChanged;
        MoveId.OnValueChanged += OnNetworkValueChanged;
        MatchId.OnValueChanged += OnNetworkValueChanged;
        XPositionX.OnValueChanged += OnNetworkValueChanged;
        XPositionY.OnValueChanged += OnNetworkValueChanged;
        YPositionX.OnValueChanged += OnNetworkValueChanged;
        YPositionY.OnValueChanged += OnNetworkValueChanged;
        XDirection.OnValueChanged += OnNetworkValueChanged;
        YDirection.OnValueChanged += OnNetworkValueChanged;
        XHasPlaced.OnValueChanged += OnNetworkValueChanged;
        YHasPlaced.OnValueChanged += OnNetworkValueChanged;
        XHasChosenDirection.OnValueChanged += OnNetworkValueChanged;
        YHasChosenDirection.OnValueChanged += OnNetworkValueChanged;
        RematchReadyMask.OnValueChanged += OnNetworkValueChanged;
        BoardCells.OnListChanged += OnBoardListChanged;
        BoardSkinIds.OnListChanged += OnBoardSkinListChanged;

        if (IsServer)
            StartCoroutine(InitializeAuthoritativeStateWhenReady());
        else
            ScheduleClientApply();
    }

    public override void OnNetworkDespawn()
    {
        currentState.OnValueChanged -= OnNetworkValueChanged;
        WinnerState.OnValueChanged -= OnNetworkValueChanged;
        StateRevision.OnValueChanged -= OnNetworkValueChanged;
        MoveId.OnValueChanged -= OnNetworkValueChanged;
        MatchId.OnValueChanged -= OnNetworkValueChanged;
        XPositionX.OnValueChanged -= OnNetworkValueChanged;
        XPositionY.OnValueChanged -= OnNetworkValueChanged;
        YPositionX.OnValueChanged -= OnNetworkValueChanged;
        YPositionY.OnValueChanged -= OnNetworkValueChanged;
        XDirection.OnValueChanged -= OnNetworkValueChanged;
        YDirection.OnValueChanged -= OnNetworkValueChanged;
        XHasPlaced.OnValueChanged -= OnNetworkValueChanged;
        YHasPlaced.OnValueChanged -= OnNetworkValueChanged;
        XHasChosenDirection.OnValueChanged -= OnNetworkValueChanged;
        YHasChosenDirection.OnValueChanged -= OnNetworkValueChanged;
        RematchReadyMask.OnValueChanged -= OnNetworkValueChanged;
        BoardCells.OnListChanged -= OnBoardListChanged;
        BoardSkinIds.OnListChanged -= OnBoardSkinListChanged;

        if (Instance == this)
            Instance = null;
    }

    private IEnumerator InitializeAuthoritativeStateWhenReady()
    {
        while (boardManager == null ||
               boardManager.CurrentBoard == null ||
               turnManager == null)
        {
            boardManager = boardManager != null ? boardManager : FindFirstObjectByType<BoardManager>();
            turnManager = turnManager != null ? turnManager : FindFirstObjectByType<TurnManager>();
            placementManager = placementManager != null ? placementManager : FindFirstObjectByType<PlacementManager>();
            gameResultManager = gameResultManager != null ? gameResultManager : FindFirstObjectByType<GameResultManager>();
            directionButtonController = directionButtonController != null ? directionButtonController : FindFirstObjectByType<DirectionButtonController>();
            yield return null;
        }

        EnsureBoardLists();
        CaptureFromServer();
        serverInitialized = true;
    }

    private void EnsureBoardLists()
    {
        if (!IsServer || boardManager == null || boardManager.CurrentBoard == null)
            return;

        int count = boardManager.CurrentBoard.Width * boardManager.CurrentBoard.Height;

        if (BoardCells.Count != count)
        {
            BoardCells.Clear();
            BoardSkinIds.Clear();

            for (int i = 0; i < count; i++)
            {
                BoardCells.Add(EmptyCell);
                BoardSkinIds.Add(new FixedString64Bytes());
            }
        }
    }

    private void OnNetworkValueChanged<T>(T previous, T current)
    {
        if (IsServer)
            return;

        ScheduleClientApply();
    }

    private void OnBoardListChanged(NetworkListEvent<byte> changeEvent)
    {
        if (IsServer)
            return;

        ScheduleClientApply();
    }

    private void OnBoardSkinListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        if (IsServer)
            return;

        ScheduleClientApply();
    }

    private void ScheduleClientApply()
    {
        if (IsServer || !IsSpawned || applyScheduled)
            return;

        applyScheduled = true;
        StartCoroutine(ApplyClientStateNextFrame());
    }

    private IEnumerator ApplyClientStateNextFrame()
    {
        yield return null;
        applyScheduled = false;
        ApplyAuthoritativeStateToClient();
    }

    public GameState GetAuthoritativeState()
    {
        return (GameState)currentState.Value;
    }

    public bool IsPhase(GameState state)
    {
        return currentState.Value == (int)state;
    }

    public bool CanRequestPlacement(PlayerType player)
    {
        if (OnlineMatchPlayerState.Instance != null &&
            !OnlineMatchPlayerState.Instance.IsMatchReady())
            return false;

        GameState state = GetAuthoritativeState();

        return (player == PlayerType.X && state == GameState.WaitingForPlayerXPlacement) ||
               (player == PlayerType.Y && state == GameState.WaitingForPlayerYPlacement);
    }

    public bool IsPlayerAllowed(ulong clientId, PlayerType player)
    {
        if (NetworkManager.Singleton == null)
            return false;

        return player == PlayerType.X
            ? clientId == NetworkManager.ServerClientId
            : player == PlayerType.Y && clientId != NetworkManager.ServerClientId;
    }

    public bool CanRequestDirection(PlayerType player)
    {
        if (OnlineMatchPlayerState.Instance != null &&
            !OnlineMatchPlayerState.Instance.IsMatchReady())
            return false;

        GameState state = GetAuthoritativeState();

        return (player == PlayerType.X && state == GameState.WaitingForPlayerXDirection) ||
               (player == PlayerType.Y && state == GameState.WaitingForPlayerYDirection);
    }

    public bool IsCurrentMatchOver()
    {
        return WinnerState.Value != NoWinner ||
               IsPhase(GameState.Won) ||
               IsPhase(GameState.Draw);
    }

    public void SetState(GameState state)
    {
        if (!IsServer)
            return;

        SetAuthoritativePhase(state);
    }

    /// <summary>
    /// Changes the authoritative match phase without reading any client-local
    /// gameplay state. Every client observes the same phase through the
    /// NetworkVariable and rebuilds presentation from the synchronized snapshot.
    /// </summary>
    public void SetAuthoritativePhase(GameState state)
    {
        if (!IsServer)
            return;

        if (currentState.Value == (int)state)
            return;

        currentState.Value = (int)state;
        StateRevision.Value++;
    }

    /// <summary>
    /// Commits a complete authoritative turn/round snapshot in one place.
    /// This prevents a later local callback from overwriting a valid network
    /// direction or phase with stale state.
    /// </summary>
    public void CommitAuthoritativePhase(
        GameState state,
        PlayerAction xAction,
        PlayerAction yAction)
    {
        if (!IsServer || xAction == null || yAction == null)
            return;

        XPositionX.Value = xAction.position.x;
        XPositionY.Value = xAction.position.y;
        YPositionX.Value = yAction.position.x;
        YPositionY.Value = yAction.position.y;

        XDirection.Value = (int)xAction.direction;
        YDirection.Value = (int)yAction.direction;

        XHasPlaced.Value = xAction.hasPlaced;
        YHasPlaced.Value = yAction.hasPlaced;
        XHasChosenDirection.Value = xAction.hasChosenDirection;
        YHasChosenDirection.Value = yAction.hasChosenDirection;

        currentState.Value = (int)state;
        StateRevision.Value++;
    }

    public int BeginMove()
    {
        if (!IsServer)
            return MoveId.Value;

        MoveId.Value++;
        StateRevision.Value++;
        return MoveId.Value;
    }

    public void SetWinner(PlayerType winner)
    {
        if (!IsServer)
            return;

        WinnerState.Value = winner == PlayerType.X ? WinnerX : WinnerY;
        currentState.Value = (int)GameState.Won;
        StateRevision.Value++;
    }

    public void SetDraw()
    {
        if (!IsServer)
            return;

        WinnerState.Value = DrawResult;
        currentState.Value = (int)GameState.Draw;
        StateRevision.Value++;
    }

    public void ClearResult()
    {
        if (!IsServer)
            return;

        WinnerState.Value = NoWinner;
    }

    public void ClearServerSkinState()
    {
        if (!IsServer)
            return;

        EnsureBoardLists();

        for (int i = 0; i < BoardSkinIds.Count; i++)
            BoardSkinIds[i] = new FixedString64Bytes();
    }

    public void RefreshClientPresentationAfterMove()
    {
        if (IsServer)
            return;

        lastAppliedRevision = -1;
        ScheduleClientApply();
    }


    public void SetRematchMask(byte mask)
    {
        if (!IsServer)
            return;

        RematchReadyMask.Value = mask;
        StateRevision.Value++;
    }

    public void ResetServerMatch()
    {
        if (!IsServer)
            return;

        if (boardManager == null)
            boardManager = FindFirstObjectByType<BoardManager>();

        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        boardManager?.ResetBoard();
        turnManager?.ResetMatch();

        EnsureBoardLists();

        for (int i = 0; i < BoardCells.Count; i++)
        {
            BoardCells[i] = EmptyCell;
            BoardSkinIds[i] = new FixedString64Bytes();
        }

        WinnerState.Value = NoWinner;
        RematchReadyMask.Value = 0;
        MoveId.Value = 0;
        MatchId.Value++;
        currentState.Value = (int)GameState.WaitingForPlayerXPlacement;
        StateRevision.Value++;
        serverInitialized = true;
    }

    public void CaptureFromServer()
    {
        if (!IsServer || !serverInitialized &&
            (boardManager == null || boardManager.CurrentBoard == null || turnManager == null))
            return;

        boardManager ??= FindFirstObjectByType<BoardManager>();
        turnManager ??= FindFirstObjectByType<TurnManager>();
        if (boardManager == null || boardManager.CurrentBoard == null || turnManager == null)
            return;

        EnsureBoardLists();

        // The server snapshot owns the board occupancy. Skin entries are
        // already stored separately and are only changed by accepted moves.
        Board board = boardManager.CurrentBoard;

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                int index = GetIndex(x, y);
                BoardCell cell = board.GetCell(x, y);

                BoardCells[index] = GetCellCode(cell);

                // Do not overwrite a valid stored skin when only the board
                // occupancy changed. Skin identity is updated explicitly on
                // placement and movement.
            }
        }

        PlayerAction xAction = turnManager.PlayerXAction;
        PlayerAction yAction = turnManager.PlayerYAction;

        XPositionX.Value = xAction.position.x;
        XPositionY.Value = xAction.position.y;
        YPositionX.Value = yAction.position.x;
        YPositionY.Value = yAction.position.y;

        XDirection.Value = (int)xAction.direction;
        YDirection.Value = (int)yAction.direction;

        XHasPlaced.Value = xAction.hasPlaced;
        YHasPlaced.Value = yAction.hasPlaced;
        XHasChosenDirection.Value = xAction.hasChosenDirection;
        YHasChosenDirection.Value = yAction.hasChosenDirection;

        // During authoritative movement/result resolution, TurnManager can
        // still be in its local presentation state. Do not let a board
        // snapshot overwrite the authoritative network phase (for example,
        // Won -> Moving or CheckingResult -> Moving).
        GameState authoritativeState = CurrentAuthoritativeState;
        bool preserveAuthoritativePhase =
            authoritativeState == GameState.RevealingDirections ||
            authoritativeState == GameState.Moving ||
            authoritativeState == GameState.CheckingResult ||
            authoritativeState == GameState.Won ||
            authoritativeState == GameState.Draw;

        if (!preserveAuthoritativePhase)
            currentState.Value = (int)turnManager.CurrentState;

        StateRevision.Value++;
    }

    public string GetSkinAt(int x, int y)
    {
        int index = GetIndex(x, y);
        if (index < 0 || index >= BoardSkinIds.Count)
            return string.Empty;

        return BoardSkinIds[index].ToString();
    }

    public void CapturePlacementSkin(int x, int y, string skinId)
    {
        if (!IsServer || boardManager?.CurrentBoard == null)
            return;

        EnsureBoardLists();

        int index = GetIndex(x, y);
        if (index < 0 || index >= BoardSkinIds.Count)
            return;

        BoardSkinIds[index] = new FixedString64Bytes(skinId ?? string.Empty);
    }

    public void MoveStoredSkin(
        int startX,
        int startY,
        int destinationX,
        int destinationY)
    {
        if (!IsServer)
            return;

        EnsureBoardLists();

        int startIndex = GetIndex(startX, startY);
        int destinationIndex = GetIndex(destinationX, destinationY);

        if (startIndex < 0 || destinationIndex < 0 ||
            startIndex >= BoardSkinIds.Count ||
            destinationIndex >= BoardSkinIds.Count)
        {
            return;
        }

        if (startIndex == destinationIndex)
            return;

        FixedString64Bytes skin = BoardSkinIds[startIndex];

        BoardSkinIds[destinationIndex] = skin;
        BoardSkinIds[startIndex] = new FixedString64Bytes();
    }

    /// <summary>
    /// Moves both round skins atomically. This is required for simultaneous
    /// movement because X and Y can legally swap positions. Both source skins
    /// are captured before either source cell is cleared.
    /// </summary>
    public void MoveStoredSkins(
        Vector2Int xStart,
        Vector2Int xDestination,
        Vector2Int yStart,
        Vector2Int yDestination)
    {
        if (!IsServer)
            return;

        EnsureBoardLists();

        int xStartIndex = GetIndex(xStart.x, xStart.y);
        int xDestinationIndex = GetIndex(xDestination.x, xDestination.y);
        int yStartIndex = GetIndex(yStart.x, yStart.y);
        int yDestinationIndex = GetIndex(yDestination.x, yDestination.y);

        if (xStartIndex < 0 || xDestinationIndex < 0 ||
            yStartIndex < 0 || yDestinationIndex < 0 ||
            xStartIndex >= BoardSkinIds.Count ||
            xDestinationIndex >= BoardSkinIds.Count ||
            yStartIndex >= BoardSkinIds.Count ||
            yDestinationIndex >= BoardSkinIds.Count)
        {
            return;
        }

        FixedString64Bytes xSkin = BoardSkinIds[xStartIndex];
        FixedString64Bytes ySkin = BoardSkinIds[yStartIndex];

        // Clear both distinct moving origins first.
        if (xStartIndex != xDestinationIndex)
            BoardSkinIds[xStartIndex] = new FixedString64Bytes();

        if (yStartIndex != yDestinationIndex &&
            yStartIndex != xStartIndex)
        {
            BoardSkinIds[yStartIndex] = new FixedString64Bytes();
        }

        // Write destinations only after both source skins have been captured.
        if (xStartIndex != xDestinationIndex)
            BoardSkinIds[xDestinationIndex] = xSkin;

        if (yStartIndex != yDestinationIndex)
            BoardSkinIds[yDestinationIndex] = ySkin;
    }

    public void ApplyAuthoritativeBoardState(
        System.Collections.Generic.IList<byte> cells,
        System.Collections.Generic.IList<FixedString64Bytes> skins)
    {
        if (!IsServer)
            return;

        EnsureBoardLists();

        int count = Mathf.Min(BoardCells.Count, cells?.Count ?? 0);
        for (int i = 0; i < count; i++)
        {
            BoardCells[i] = cells[i];
            if (skins != null && i < skins.Count)
                BoardSkinIds[i] = skins[i];
        }
    }

    public Vector2Int GetStoredPosition(PlayerType player)
    {
        return player == PlayerType.X
            ? new Vector2Int(XPositionX.Value, XPositionY.Value)
            : new Vector2Int(YPositionX.Value, YPositionY.Value);
    }

    public bool IsLocalPlayerSide(PlayerType player)
    {
        if (NetworkManager.Singleton == null)
            return player == PlayerType.X;

        if (OnlineMatchPlayerState.Instance != null &&
            OnlineMatchPlayerState.Instance.IsClientAssigned(
                NetworkManager.Singleton.LocalClientId))
        {
            return OnlineMatchPlayerState.Instance.IsLocalSide(player);
        }

        return NetworkManager.Singleton.IsServer
            ? player == PlayerType.X
            : player == PlayerType.Y;
    }

    private void ApplyAuthoritativeStateToClient()
    {
        if (IsServer)
            return;

        boardManager ??= FindFirstObjectByType<BoardManager>();
        turnManager ??= FindFirstObjectByType<TurnManager>();
        placementManager ??= FindFirstObjectByType<PlacementManager>();
        gameResultManager ??= FindFirstObjectByType<GameResultManager>();

        if (boardManager == null || boardManager.CurrentBoard == null || turnManager == null)
            return;

        if (lastAppliedRevision == StateRevision.Value)
            return;

        lastAppliedRevision = StateRevision.Value;

        boardManager.ApplySnapshot(BoardCells, BoardSkinIds);

        turnManager.ApplyAuthoritativeState(
            (GameState)currentState.Value,
            new Vector2Int(XPositionX.Value, XPositionY.Value),
            new Vector2Int(YPositionX.Value, YPositionY.Value),
            (MoveDirection)XDirection.Value,
            (MoveDirection)YDirection.Value,
            XHasPlaced.Value,
            YHasPlaced.Value,
            XHasChosenDirection.Value,
            YHasChosenDirection.Value);

        int winner = WinnerState.Value;

        if (gameResultManager != null)
        {
                if (winner == WinnerX)
                gameResultManager.SetWinner(PlayerType.X);
            else if (winner == WinnerY)
                gameResultManager.SetWinner(PlayerType.Y);
            else if (winner == DrawResult)
                gameResultManager.SetDraw();
            else if (winner == NoWinner && gameResultManager.HasResult)
                gameResultManager.ResetResult();
        }

        GameState state = (GameState)currentState.Value;
        bool safeToRebuildVisuals =
            state == GameState.WaitingForPlayerXPlacement ||
            state == GameState.WaitingForPlayerYPlacement ||
            state == GameState.WaitingForPlayerXDirection ||
            state == GameState.WaitingForPlayerYDirection;

        if (safeToRebuildVisuals && placementManager != null)
        {
            placementManager.RebuildFromNetworkState(
                BoardCells,
                BoardSkinIds,
                new Vector2Int(XPositionX.Value, XPositionY.Value),
                new Vector2Int(YPositionX.Value, YPositionY.Value),
                XHasPlaced.Value,
                YHasPlaced.Value);
        }

        directionButtonController?.RefreshForCurrentTurn();
    }

    private byte GetCellCode(BoardCell cell)
    {
        if (cell == null || cell.IsEmpty)
            return EmptyCell;

        return cell.Occupant == PlayerType.X
            ? PlayerXCell
            : PlayerYCell;
    }

    private int GetIndex(int x, int y)
    {
        if (boardManager?.CurrentBoard == null)
            return -1;

        return x * boardManager.CurrentBoard.Height + y;
    }
}
