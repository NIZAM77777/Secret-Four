using Unity.Collections;
using Unity.Netcode;

public sealed class OnlineMatchPlayerState : NetworkBehaviour
{
    public static OnlineMatchPlayerState Instance { get; private set; }

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

    public NetworkVariable<bool> MatchReady { get; } =
        new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        Instance = this;

        NetworkManager.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

        if (IsServer)
        {
            HostPlayerClientId.Value = NetworkManager.ServerClientId;
            HostPlayerName.Value = new FixedString64Bytes(
                GetLocalPlayerNameOrDefault());

            ApplyLocalHostSkin();
            RefreshReadyState();
        }
        else
        {
            SubmitLocalJoinerProfile();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsSpawned)
            return;

        if (IsServer)
        {
            if (clientId != NetworkManager.ServerClientId &&
                JoinerPlayerClientId.Value == ulong.MaxValue)
            {
                // The joining client submits its own profile immediately after spawn.
            }
            return;
        }

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

        SubmitJoinerProfileRpc(
            playerName,
            skinId ?? string.Empty);
    }

    private void RefreshReadyState()
    {
        if (!IsServer)
            return;

        MatchReady.Value =
            HostPlayerClientId.Value != ulong.MaxValue &&
            JoinerPlayerClientId.Value != ulong.MaxValue;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

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

        MatchReady.Value =
            HostPlayerClientId.Value != ulong.MaxValue &&
            JoinerPlayerClientId.Value != ulong.MaxValue;
    }

    public PlayerType GetSideForClient(ulong clientId)
    {
        if (clientId == HostPlayerClientId.Value)
            return PlayerType.X;

        if (clientId == JoinerPlayerClientId.Value)
            return PlayerType.Y;

        return PlayerType.X;
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

    private string GetLocalPlayerNameOrDefault()
    {
        return PlayerNameManager.GetPlayerName();
    }

    private void ApplyLocalHostSkin()
    {
        string skinId =
            SecretFourStoreIntegration.GetSelectedPlayerItemId(PlayerType.X);

        HostSkinId.Value =
            new FixedString128Bytes(skinId ?? string.Empty);
    }
}