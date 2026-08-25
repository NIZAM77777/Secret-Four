[System.Serializable]
public class PlayerData
{
    public PlayerType playerType;
    public string playerName;

    public PlayerData(PlayerType playerType, string playerName)
    {
        this.playerType = playerType;
        this.playerName = playerName;
    }
}