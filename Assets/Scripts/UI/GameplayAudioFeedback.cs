using SecretFour.Localization;
using UnityEngine;
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
            if (current == GameState.WaitingForPlayerXDirection || current == GameState.WaitingForPlayerYDirection) GameAudio.Play("place");
            else if (current == GameState.RevealingDirections) GameAudio.Play("reveal");
            else if (current == GameState.Won) GameAudio.Play("win");
            else if (current == GameState.Draw) GameAudio.Play("draw");
        }
        last = current;
        initialized = true;
    }
}
