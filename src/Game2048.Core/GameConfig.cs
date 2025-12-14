namespace Game2048.Core;

/// <summary>
/// Configuration settings for the 2048 game.
/// </summary>
public class GameConfig
{
    /// <summary>
    /// Gets or sets the size of the game board (default: 4x4).
    /// </summary>
    public int Size { get; set; } = 4;

    /// <summary>
    /// Gets or sets the tile value required to win (default: 2048).
    /// </summary>
    public int WinTile { get; set; } = 2048;

    /// <summary>
    /// Gets or sets whether the game allows continuing after winning (default: true for endless mode).
    /// </summary>
    public bool AllowContinueAfterWin { get; set; } = true;
}
