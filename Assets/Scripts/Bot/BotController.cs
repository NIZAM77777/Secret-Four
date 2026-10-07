using System.Collections;
using UnityEngine;

public class BotController : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private DirectionSelector directionSelector;
    [SerializeField] private EasyBot easyBot;
    [SerializeField] private MediumBot mediumBot;
    [SerializeField] private HardBot hardBot;
    [SerializeField, Min(0)] private float thinkingDelay = 0.8f;
    private bool botThinking;

    private bool IsBotMatch => GameSession.Instance != null &&
        GameSession.Instance.CurrentGameMode == GameMode.Bot &&
        !(Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening);

    private void Update()
    {
        if (IsBotMatch && !botThinking && Time.timeScale > 0 && turnManager != null &&
            turnManager.CurrentState == GameState.WaitingForPlayerYPlacement) StartCoroutine(TakeTurn());
    }
    private IEnumerator TakeTurn()
    {
        botThinking = true;
        yield return new WaitForSeconds(thinkingDelay);
        if (!IsBotMatch || !turnManager.CanPlaceCoin(PlayerType.Y)) { botThinking = false; yield break; }
        Board board = boardManager.CurrentBoard;
        if (board == null || board.IsFull()) { botThinking = false; yield break; }
        Vector2Int x = turnManager.PlayerXAction.position;
        BotDifficulty difficulty = GameSession.Instance.CurrentBotDifficulty;
        Vector2Int position;
        switch (difficulty)
        {
            case BotDifficulty.Medium when mediumBot != null: position = mediumBot.ChoosePosition(board, x); break;
            case BotDifficulty.Hard when hardBot != null: position = hardBot.GetBestPlacement(board, null); break;
            default: position = easyBot != null ? easyBot.ChoosePosition(board) : BotPlanner.Choose(board, x, difficulty).position; break;
        }
        if (!placementManager.ApplyCoinPlacement(position.x, position.y, PlayerType.Y)) { botThinking = false; yield break; }
        yield return new WaitForSeconds(thinkingDelay);
        if (IsBotMatch && turnManager.CanChooseDirection(PlayerType.Y))
        {
            MoveDirection direction;
            switch (difficulty)
            {
                case BotDifficulty.Medium when mediumBot != null: direction = mediumBot.ChooseDirection(board, position, x); break;
                case BotDifficulty.Hard when hardBot != null: direction = hardBot.GetBestDirection(SecretFourRules.LegalDirections(board, position, x)); break;
                default: direction = easyBot != null ? easyBot.ChooseDirection(board, position, x) : MoveDirection.Stay; break;
            }
            directionSelector.ApplyDirection(PlayerType.Y, direction);
        }
        botThinking = false;
    }
    public void ResetBot()
    {
        StopAllCoroutines();
        botThinking = false;
    }
    private void OnDisable() => ResetBot();
}
