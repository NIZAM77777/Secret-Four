using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class MovementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private GameResultManager gameResultManager;
    [SerializeField] private WinConditionChecker winConditionChecker;
    [SerializeField] private MovementRuleResolver movementRuleResolver;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardView boardView;
    [SerializeField] private NetworkMovementManager networkMovementManager;
    [SerializeField] private NetworkGameResultManager networkGameResultManager;
    [SerializeField] private PlacementManager placementManager;
    [Header("Timing")]
    [SerializeField, Min(0)] private float revealDelay = 2f;
    [SerializeField, Min(0)] private float movementDuration = 0.3f;
    [SerializeField, Min(0)] private float blockedPauseDuration = 0.08f;
    [SerializeField, Min(0)] private float returnDuration = 0.16f;
    private bool movementInProgress;
    private GameObject playerXCoin;
    private GameObject playerYCoin;
    public bool IsMovementInProgress => movementInProgress;
    private bool IsOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    public void StartMovementSequence(GameObject xCoin, GameObject yCoin)
    {
        if (movementInProgress) return;
        if (IsOnline) { networkMovementManager.RequestMovementResolution(); return; }
        Begin(movementRuleResolver.Resolve(turnManager.PlayerXAction, turnManager.PlayerYAction), xCoin, yCoin, -1);
    }
    public void ApplyNetworkResolution(MovementResolution resolution, GameObject xCoin, GameObject yCoin, int moveId = -1)
    {
        if (!movementInProgress) Begin(resolution, xCoin, yCoin, moveId);
    }
    private void Begin(MovementResolution resolution, GameObject xCoin, GameObject yCoin, int moveId)
    {
        playerXCoin = xCoin;
        playerYCoin = yCoin;
        movementInProgress = true;
        StartCoroutine(ResolveRound(resolution, moveId));
    }
    private IEnumerator ResolveRound(MovementResolution resolution, int moveId)
    {
        turnManager.SetState(GameState.RevealingDirections);
        yield return new WaitForSeconds(revealDelay);
        turnManager.SetState(GameState.Moving);
        Vector3 xs = World(resolution.playerXResult.startPosition);
        Vector3 ys = World(resolution.playerYResult.startPosition);
        Vector3 xt = World(resolution.playerXResult.destination);
        Vector3 yt = World(resolution.playerYResult.destination);
        yield return Animate(xs, xt, ys, yt, movementDuration);
        if (resolution.AnyCollision)
        {
            yield return new WaitForSeconds(blockedPauseDuration);
            yield return Animate(xt, World(SecretFourRules.FinalPosition(resolution.playerXResult, resolution.playerXCollision)),
                yt, World(SecretFourRules.FinalPosition(resolution.playerYResult, resolution.playerYCollision)), returnDuration);
        }
        if (!IsOnline || NetworkManager.Singleton.IsServer) SecretFourRules.Apply(boardManager.CurrentBoard, resolution);
        placementManager.UpdateCoinPositions(resolution, playerXCoin, playerYCoin);
        if (IsOnline)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkGameState.Instance.MoveStoredSkins(resolution.playerXResult.startPosition,
                    SecretFourRules.FinalPosition(resolution.playerXResult, resolution.playerXCollision),
                    resolution.playerYResult.startPosition, SecretFourRules.FinalPosition(resolution.playerYResult, resolution.playerYCollision));
                networkGameResultManager.ResolveAfterAuthoritativeMovement();
                networkMovementManager.CompleteAuthoritativeMove(moveId);
            }
            movementInProgress = false;
            NetworkGameState.Instance?.RefreshClientPresentationAfterMove();
        }
        else
        {
            turnManager.SetState(GameState.CheckingResult);
            if (winConditionChecker.HasWinner(out PlayerType winner)) gameResultManager.SetWinner(winner);
            else if (winConditionChecker.HasSimultaneousWin || winConditionChecker.IsBoardFull()) gameResultManager.SetDraw();
            else turnManager.StartNextRound();
            movementInProgress = false;
        }
    }
    private Vector3 World(Vector2Int position) => boardView.GetCellWorldPosition(position.x, position.y);
    private IEnumerator Animate(Vector3 xs, Vector3 xe, Vector3 ys, Vector3 ye, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
            if (playerXCoin != null) playerXCoin.transform.position = Vector3.Lerp(xs, xe, t);
            if (playerYCoin != null) playerYCoin.transform.position = Vector3.Lerp(ys, ye, t);
            yield return null;
        }
        if (playerXCoin != null) playerXCoin.transform.position = xe;
        if (playerYCoin != null) playerYCoin.transform.position = ye;
    }
    public void ResetNetworkMovementState()
    {
        StopAllCoroutines();
        movementInProgress = false;
        playerXCoin = null;
        playerYCoin = null;
    }
}
