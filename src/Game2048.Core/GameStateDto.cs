namespace Game2048.Core;

/// <summary>
/// Serializable DTO for persisting game state.
/// </summary>
public class GameStateDto
{
    /// <summary>
    /// Version for future compatibility.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Board size.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Flat array of board values.
    /// </summary>
    public int[] Board { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Current score.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Number of moves made.
    /// </summary>
    public int MoveCount { get; set; }

    /// <summary>
    /// Whether the player has won.
    /// </summary>
    public bool IsWon { get; set; }

    /// <summary>
    /// Whether the game is over.
    /// </summary>
    public bool IsGameOver { get; set; }

    /// <summary>
    /// Converts a GameState to a DTO.
    /// </summary>
    public static GameStateDto FromGameState(GameState state)
    {
        return new GameStateDto
        {
            Size = state.Size,
            Board = (int[])state.Board.Clone(),
            Score = state.Score,
            MoveCount = state.MoveCount,
            IsWon = state.IsWon,
            IsGameOver = state.IsGameOver
        };
    }

    /// <summary>
    /// Converts the DTO to a GameState.
    /// </summary>
    public GameState ToGameState()
    {
        return new GameState(Size)
        {
            Board = (int[])Board.Clone(),
            Score = Score,
            MoveCount = MoveCount,
            IsWon = IsWon,
            IsGameOver = IsGameOver
        };
    }
}
