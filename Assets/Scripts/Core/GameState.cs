public enum GameState
{
    WaitingForPlayerXPlacement,
    WaitingForPlayerXDirection,

    WaitingForPlayerYPlacement,
    WaitingForPlayerYDirection,

    RevealingDirections,

    Moving,

    CheckingResult,

    Won,

    Draw
}