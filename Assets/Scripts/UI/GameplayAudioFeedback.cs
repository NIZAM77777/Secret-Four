using SecretFour.Localization;
using UnityEngine;

/// <summary>
/// Turns match state changes into sounds:
/// coin placed → "place", direction chosen → "lock", reveal → "reveal", coins moving → "move".
/// Win / lose / draw are played by GameOverUI when the result screen appears.
/// </summary>
public class GameplayAudioFeedback : MonoBehaviour
{
    private TurnManager turn;
    private GameState last;
    private bool initialized;

    private void Update()
    {
        if (turn == null) turn = FindFirstObjectByType<TurnManager>();
        if (turn == null) return;

        GameState current = turn.CurrentState;
        if (initialized && current != last)
        {
            bool wasChoosing = last == GameState.WaitingForPlayerXDirection || last == GameState.WaitingForPlayerYDirection;

            if (current == GameState.WaitingForPlayerXDirection || current == GameState.WaitingForPlayerYDirection)
                GameAudio.Play("place");
            else if (current == GameState.RevealingDirections)
                GameAudio.Play("reveal");
            else if (current == GameState.Moving)
                GameAudio.Play("move");
            else if (wasChoosing && (current == GameState.WaitingForPlayerXPlacement || current == GameState.WaitingForPlayerYPlacement))
                GameAudio.Play("lock");
        }

        last = current;
        initialized = true;
    }
}
