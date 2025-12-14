namespace Game2048.Core;

/// <summary>
/// Main game engine for the 2048 game.
/// </summary>
public class GameEngine
{
    private readonly GameConfig _config;
    private readonly IRandomSource _random;
    private GameState _currentState;
    private readonly Stack<GameState> _undoStack = new();
    private readonly Stack<GameState> _redoStack = new();
    private const int MaxHistorySize = 50;

    public GameState CurrentState => _currentState;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public GameEngine(GameConfig config, IRandomSource randomSource)
    {
        _config = config;
        _random = randomSource;
        _currentState = new GameState(_config.Size);
        InitializeGame();
    }

    /// <summary>
    /// Starts a new game.
    /// </summary>
    public void NewGame()
    {
        _currentState = new GameState(_config.Size);
        _undoStack.Clear();
        _redoStack.Clear();
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Spawn two initial tiles
        SpawnTile();
        SpawnTile();
    }

    /// <summary>
    /// Attempts to move in the specified direction.
    /// </summary>
    /// <returns>True if the move changed the board, false otherwise.</returns>
    public bool Move(Direction direction)
    {
        if (_currentState.IsGameOver)
            return false;

        var previousState = _currentState.Clone();
        bool moved = false;

        switch (direction)
        {
            case Direction.Up:
                moved = MoveUp();
                break;
            case Direction.Down:
                moved = MoveDown();
                break;
            case Direction.Left:
                moved = MoveLeft();
                break;
            case Direction.Right:
                moved = MoveRight();
                break;
        }

        if (moved)
        {
            // Save to undo stack with bounded capacity
            if (_undoStack.Count >= MaxHistorySize)
            {
                // Remove oldest item - convert to list temporarily for this operation
                var tempList = _undoStack.ToList();
                tempList.RemoveAt(0);
                _undoStack.Clear();
                foreach (var state in tempList.AsEnumerable().Reverse())
                    _undoStack.Push(state);
            }
            _undoStack.Push(previousState);

            // Clear redo stack on new move
            _redoStack.Clear();

            // Increment move count
            _currentState.MoveCount++;

            // Spawn new tile
            SpawnTile();

            // Check win condition
            if (!_currentState.IsWon && HasTileWithValue(_config.WinTile))
            {
                _currentState.IsWon = true;
            }

            // Check game over
            if (!_config.AllowContinueAfterWin && _currentState.IsWon)
            {
                _currentState.IsGameOver = true;
            }
            else if (IsGameOverCondition())
            {
                _currentState.IsGameOver = true;
            }
        }
        else
        {
            // Even if no move was made, check if game is over
            if (IsGameOverCondition())
            {
                _currentState.IsGameOver = true;
            }
        }

        return moved;
    }

    private bool MoveLeft()
    {
        bool moved = false;
        int scoreGained = 0;

        for (int row = 0; row < _currentState.Size; row++)
        {
            var line = GetRow(row);
            var (newLine, lineMoved, lineScore) = ProcessLine(line);
            if (lineMoved)
            {
                SetRow(row, newLine);
                moved = true;
                scoreGained += lineScore;
            }
        }

        if (moved)
        {
            _currentState.Score += scoreGained;
        }

        return moved;
    }

    private bool MoveRight()
    {
        bool moved = false;
        int scoreGained = 0;

        for (int row = 0; row < _currentState.Size; row++)
        {
            var line = GetRow(row);
            Array.Reverse(line);
            var (newLine, lineMoved, lineScore) = ProcessLine(line);
            if (lineMoved)
            {
                Array.Reverse(newLine);
                SetRow(row, newLine);
                moved = true;
                scoreGained += lineScore;
            }
        }

        if (moved)
        {
            _currentState.Score += scoreGained;
        }

        return moved;
    }

    private bool MoveUp()
    {
        bool moved = false;
        int scoreGained = 0;

        for (int col = 0; col < _currentState.Size; col++)
        {
            var line = GetColumn(col);
            var (newLine, lineMoved, lineScore) = ProcessLine(line);
            if (lineMoved)
            {
                SetColumn(col, newLine);
                moved = true;
                scoreGained += lineScore;
            }
        }

        if (moved)
        {
            _currentState.Score += scoreGained;
        }

        return moved;
    }

    private bool MoveDown()
    {
        bool moved = false;
        int scoreGained = 0;

        for (int col = 0; col < _currentState.Size; col++)
        {
            var line = GetColumn(col);
            Array.Reverse(line);
            var (newLine, lineMoved, lineScore) = ProcessLine(line);
            if (lineMoved)
            {
                Array.Reverse(newLine);
                SetColumn(col, newLine);
                moved = true;
                scoreGained += lineScore;
            }
        }

        if (moved)
        {
            _currentState.Score += scoreGained;
        }

        return moved;
    }

    /// <summary>
    /// Processes a line (compress and merge).
    /// Returns the new line, whether it changed, and the score gained.
    /// </summary>
    private (int[] newLine, bool moved, int score) ProcessLine(int[] line)
    {
        var result = new int[line.Length];
        int resultIndex = 0;
        int score = 0;
        bool moved = false;

        // First, compress non-zero values
        var nonZero = line.Where(x => x != 0).ToArray();

        // Then merge adjacent equal values
        int i = 0;
        while (i < nonZero.Length)
        {
            if (i + 1 < nonZero.Length && nonZero[i] == nonZero[i + 1])
            {
                // Merge
                int mergedValue = nonZero[i] * 2;
                result[resultIndex++] = mergedValue;
                score += mergedValue;
                i += 2; // Skip the next tile as it was merged
            }
            else
            {
                // No merge
                result[resultIndex++] = nonZero[i];
                i++;
            }
        }

        // Check if the line changed
        for (i = 0; i < line.Length; i++)
        {
            if (line[i] != result[i])
            {
                moved = true;
                break;
            }
        }

        return (result, moved, score);
    }

    private void SpawnTile()
    {
        var emptyPositions = new List<(int row, int col)>();

        for (int row = 0; row < _currentState.Size; row++)
        {
            for (int col = 0; col < _currentState.Size; col++)
            {
                if (_currentState.GetTile(row, col) == 0)
                {
                    emptyPositions.Add((row, col));
                }
            }
        }

        if (emptyPositions.Count == 0)
            return;

        var pos = emptyPositions[_random.Next(emptyPositions.Count)];
        int value = _random.NextDouble() < 0.9 ? 2 : 4;

        _currentState.SetTile(pos.row, pos.col, value);
    }

    private bool HasTileWithValue(int value)
    {
        for (int i = 0; i < _currentState.Board.Length; i++)
        {
            if (_currentState.Board[i] >= value)
                return true;
        }
        return false;
    }

    private bool IsGameOverCondition()
    {
        // Check if there are any empty cells
        for (int i = 0; i < _currentState.Board.Length; i++)
        {
            if (_currentState.Board[i] == 0)
                return false;
        }

        // Check if any adjacent cells can merge
        for (int row = 0; row < _currentState.Size; row++)
        {
            for (int col = 0; col < _currentState.Size; col++)
            {
                int value = _currentState.GetTile(row, col);

                // Check right neighbor
                if (col + 1 < _currentState.Size && _currentState.GetTile(row, col + 1) == value)
                    return false;

                // Check down neighbor
                if (row + 1 < _currentState.Size && _currentState.GetTile(row + 1, col) == value)
                    return false;
            }
        }

        return true;
    }

    private int[] GetRow(int row)
    {
        var result = new int[_currentState.Size];
        for (int col = 0; col < _currentState.Size; col++)
        {
            result[col] = _currentState.GetTile(row, col);
        }
        return result;
    }

    private void SetRow(int row, int[] values)
    {
        for (int col = 0; col < _currentState.Size; col++)
        {
            _currentState.SetTile(row, col, values[col]);
        }
    }

    private int[] GetColumn(int col)
    {
        var result = new int[_currentState.Size];
        for (int row = 0; row < _currentState.Size; row++)
        {
            result[row] = _currentState.GetTile(row, col);
        }
        return result;
    }

    private void SetColumn(int col, int[] values)
    {
        for (int row = 0; row < _currentState.Size; row++)
        {
            _currentState.SetTile(row, col, values[row]);
        }
    }

    /// <summary>
    /// Undoes the last move.
    /// </summary>
    public bool Undo()
    {
        if (_undoStack.Count == 0)
            return false;

        var previousState = _undoStack.Pop();

        if (_redoStack.Count >= MaxHistorySize)
        {
            // Remove oldest item
            var tempList = _redoStack.ToList();
            tempList.RemoveAt(0);
            _redoStack.Clear();
            foreach (var state in tempList.AsEnumerable().Reverse())
                _redoStack.Push(state);
        }
        _redoStack.Push(_currentState);

        _currentState = previousState;
        return true;
    }

    /// <summary>
    /// Redoes the last undone move.
    /// </summary>
    public bool Redo()
    {
        if (_redoStack.Count == 0)
            return false;

        var nextState = _redoStack.Pop();

        if (_undoStack.Count >= MaxHistorySize)
        {
            var tempList = _undoStack.ToList();
            tempList.RemoveAt(0);
            _undoStack.Clear();
            foreach (var state in tempList.AsEnumerable().Reverse())
                _undoStack.Push(state);
        }
        _undoStack.Push(_currentState);

        _currentState = nextState;
        return true;
    }

    /// <summary>
    /// Loads a game state from a DTO asynchronously.
    /// </summary>
    public Task LoadStateAsync(GameStateDto dto)
    {
        return Task.Run(() =>
        {
            _currentState = dto.ToGameState();
            _undoStack.Clear();
            _redoStack.Clear();
        });
    }

    /// <summary>
    /// Saves the current game state to a DTO asynchronously.
    /// </summary>
    public Task<GameStateDto> SaveStateAsync()
    {
        return Task.Run(() => GameStateDto.FromGameState(_currentState));
    }
}
