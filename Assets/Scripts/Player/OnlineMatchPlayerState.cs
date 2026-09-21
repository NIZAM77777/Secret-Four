using Unity.Collections;
using Unity.Netcode;

public sealed class OnlineMatchPlayerState : NetworkBehaviour
{
    public static OnlineMatchPlayerState Instance { get; private set; }
    public static event System.Action OnAnyProfileChanged;

    public NetworkVariable<ulong> HostPlayerClientId { get; } =
        new NetworkVariable<ulong>(ulong.MaxValue);

    public NetworkVariable<ulong> JoinerPlayerClientId { get; } =
        new NetworkVariable<ulong>(ulong.MaxValue);

    public NetworkVariable<FixedString64Bytes> HostPlayerName { get; } =
        new NetworkVariable<FixedString64Bytes>(
            new FixedString64Bytes("Player 1"));

    public NetworkVariable<FixedString64Bytes> JoinerPlayerName { get; } =
        new NetworkVariable<FixedString64Bytes>(
            new FixedString64Bytes("Player 2"));

    public NetworkVariable<FixedString128Bytes> HostSkinId { get; } =
        new NetworkVariable<FixedString128Bytes>();

    public NetworkVariable<FixedString128Bytes> JoinerSkinId { get; } =
        new NetworkVariable<FixedString128Bytes>();

    // The host owns the room board appearance. Both devices use this value
    // in OnlineRoom; neither device reads its own local BOARD selection there.
    public NetworkVariable<FixedString128Bytes> HostBoardSkinId { get; } =
        new NetworkVariable<FixedString128Bytes>();

    public NetworkVariable<bool> MatchReady { get; } =
        new NetworkVariable<bool>(false);

    public NetworkVariable<bool> HostReady { get; } =
        new NetworkVariable<bool>(false);

    public NetworkVariable<bool> JoinerReady { get; } =
        new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        Instance = this;

        // The network scene can be loaded by the host for both devices, so
        // every process must explicitly enter OnlineRoom mode here. This
        // prevents the client from retaining a previous Bot/Easy session.
        if (GameSession.Instance != null)
            GameSession.Instance.StartOnlineRoomGame();

        NetworkManager.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

        HostPlayerClientId.OnValueChanged += OnProfileValueChanged;
        JoinerPlayerClientId.OnValueChanged += OnProfileValueChanged;
        HostPlayerName.OnValueChanged += OnProfileValueChanged;
        JoinerPlayerName.OnValueChanged += OnProfileValueChanged;
        HostSkinId.OnValueChanged += OnProfileValueChanged;
        JoinerSkinId.OnValueChanged += OnProfileValueChanged;
        HostBoardSkinId.OnValueChanged += OnProfileValueChanged;
        HostBoardSkinId.OnValueChanged += OnProfileValueChanged;
        MatchReady.OnValueChanged += OnProfileValueChanged;

        if (IsServer)
        {
            HostPlayerClientId.Value = NetworkManager.ServerClientId;
            HostPlayerName.Value = new FixedString64Bytes(
                GetLocalPlayerNameOrDefault());

            ApplyLocalHostSkin();
            ApplyLocalHostBoardSkin();
            RefreshReadyState();
        }
        else
        {
            SubmitLocalJoinerProfile();
        }

        NotifyProfileChanged();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsSpawned)
            return;

        if (IsServer)
            return;

        if (clientId == NetworkManager.LocalClientId)
            SubmitLocalJoinerProfile();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer)
            return;

        if (clientId == JoinerPlayerClientId.Value)
        {
            JoinerPlayerClientId.Value = ulong.MaxValue;
            JoinerPlayerName.Value = new FixedString64Bytes("Player 2");
            JoinerSkinId.Value = new FixedString128Bytes();
            JoinerReady.Value = false;
            RefreshReadyState();
        }
    }

    private void SubmitLocalJoinerProfile()
    {
        if (NetworkManager.Singleton == null ||
            NetworkManager.Singleton.IsServer)
            return;

        string playerName = PlayerNameManager.GetPlayerName();

        string skinId =
            SecretFourStoreIntegration.GetSelectedPlayerItemId(PlayerType.Y);

        // The joiner's role is fixed by the network protocol:
        // host = X/LEFT, joining client = Y/RIGHT.
        SubmitJoinerProfileRpc(
            playerName,
            skinId ?? string.Empty);
    }

    private void RefreshReadyState()
    {
        if (!IsServer)
            return;

        HostReady.Value =
            HostPlayerClientId.Value != ulong.MaxValue;
        MatchReady.Value =
            HostReady.Value && JoinerReady.Value;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        HostPlayerClientId.OnValueChanged -= OnProfileValueChanged;
        JoinerPlayerClientId.OnValueChanged -= OnProfileValueChanged;
        HostPlayerName.OnValueChanged -= OnProfileValueChanged;
        JoinerPlayerName.OnValueChanged -= OnProfileValueChanged;
        HostSkinId.OnValueChanged -= OnProfileValueChanged;
        JoinerSkinId.OnValueChanged -= OnProfileValueChanged;
        HostBoardSkinId.OnValueChanged -= OnProfileValueChanged;
        MatchReady.OnValueChanged -= OnProfileValueChanged;

        if (Instance == this)
            Instance = null;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitJoinerProfileRpc(
        string playerName,
        string skinId,
        RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        if (senderId == NetworkManager.ServerClientId)
            return;

        if (JoinerPlayerClientId.Value != ulong.MaxValue &&
            JoinerPlayerClientId.Value != senderId)
            return;

        JoinerPlayerClientId.Value = senderId;
        JoinerPlayerName.Value = new FixedString64Bytes(
            string.IsNullOrWhiteSpace(playerName) ? "Player 2" : playerName.Trim());
        JoinerSkinId.Value = new FixedString128Bytes(skinId ?? string.Empty);
        JoinerReady.Value = true;
        RefreshReadyState();
        NotifyProfileChanged();
    }

    public PlayerType GetSideForClient(ulong clientId)
    {
        if (clientId == HostPlayerClientId.Value)
            return PlayerType.X;

        if (clientId == JoinerPlayerClientId.Value)
            return PlayerType.Y;

        return PlayerType.X;
    }

    public bool IsClientAssigned(ulong clientId)
    {
        return clientId == HostPlayerClientId.Value ||
               clientId == JoinerPlayerClientId.Value;
    }

    public bool IsMatchReady()
    {
        return MatchReady.Value &&
               HostPlayerClientId.Value != ulong.MaxValue &&
               JoinerPlayerClientId.Value != ulong.MaxValue;
    }

    public string GetName(PlayerType player)
    {
        return player == PlayerType.X
            ? HostPlayerName.Value.ToString()
            : JoinerPlayerName.Value.ToString();
    }

    public string GetSkinId(PlayerType player)
    {
        return player == PlayerType.X
            ? HostSkinId.Value.ToString()
            : JoinerSkinId.Value.ToString();
    }

    public bool IsLocalSide(PlayerType player)
    {
        if (NetworkManager.Singleton == null)
            return player == PlayerType.X;

        return GetSideForClient(
            NetworkManager.Singleton.LocalClientId) == player;
    }

    public System.Action OnProfileChanged;

    private void OnProfileValueChanged<T>(T previous, T current)
    {
        NotifyProfileChanged();
    }

    private void NotifyProfileChanged()
    {
        OnProfileChanged?.Invoke();
        OnAnyProfileChanged?.Invoke();
    }

    private string GetLocalPlayerNameOrDefault()
    {
        return PlayerNameManager.GetPlayerName();
    }

    public string GetBoardSkinId()
    {
        return HostBoardSkinId.Value.ToString();
    }

    private void ApplyLocalHostSkin()
    {
        string skinId =
            SecretFourStoreIntegration.GetSelectedPlayerItemId(PlayerType.X);

        HostSkinId.Value =
            new FixedString128Bytes(skinId ?? string.Empty);
    }

    private void ApplyLocalHostBoardSkin()
    {
        string boardSkinId =
            SecretFourStoreIntegration.GetSelectedBoardItemId();

        HostBoardSkinId.Value =
            new FixedString128Bytes(boardSkinId ?? string.Empty);
    }
}