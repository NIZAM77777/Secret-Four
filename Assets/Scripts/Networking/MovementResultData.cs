using Unity.Netcode;

public struct MovementResultData : INetworkSerializable
{
    public int playerType;

    public int startX;
    public int startY;

    public int destinationX;
    public int destinationY;

    public int direction;


    public void NetworkSerialize<T>(
        BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(
            ref playerType
        );

        serializer.SerializeValue(
            ref startX
        );

        serializer.SerializeValue(
            ref startY
        );

        serializer.SerializeValue(
            ref destinationX
        );

        serializer.SerializeValue(
            ref destinationY
        );

        serializer.SerializeValue(
            ref direction
        );
    }
}