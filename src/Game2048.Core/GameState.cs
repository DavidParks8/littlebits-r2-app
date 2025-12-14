namespace Game2048.Core;

/// <summary>
/// Represents the state of a 2048 game at a point in time.
/// </summary>
public class GameState
{
    /// <summary>
    /// Gets the board size (e.g., 4 for a 4x4 board).
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Gets the flat array of board values (row-major order).
    /// </summary>
    public int[] Board { get; set; }

    /// <summary>
    /// Gets the current score.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Gets the number of moves made.
    /// </summary>
    public int MoveCount { get; set; }

    /// <summary>
    /// Gets whether the player has won (reached the win tile).
    /// </summary>
    public bool IsWon { get; set; }

    /// <summary>
    /// Gets whether the game is over (no moves possible).
    /// </summary>
    public bool IsGameOver { get; set; }

    public GameState(int size)
    {
        Size = size;
        Board = new int[size * size];
        Score = 0;
        MoveCount = 0;
        IsWon = false;
        IsGameOver = false;
    }

    /// <summary>
    /// Creates a copy of the game state with specified changes.
    /// </summary>
    public GameState Clone()
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

    /// <summary>
    /// Gets the value at the specified row and column.
    /// </summary>
    public int GetTile(int row, int col)
    {
        return Board[row * Size + col];
    }

    /// <summary>
    /// Sets the value at the specified row and column.
    /// </summary>
    public void SetTile(int row, int col, int value)
    {
        Board[row * Size + col] = value;
    }
}
