using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game2048.Core;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Game2048.Maui.ViewModels;

public partial class GameViewModel : ObservableObject
{
    private readonly GameEngine _engine;
    private readonly GameConfig _config;
    private readonly IRandomSource _randomSource;

    [ObservableProperty]
    private int score;

    [ObservableProperty]
    private int bestScore;

    [ObservableProperty]
    private int moveCount;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private bool canUndo;

    [ObservableProperty]
    private bool canRedo;

    [ObservableProperty]
    private ObservableCollection<TileViewModel> tiles = [];

    public GameViewModel(GameConfig config, IRandomSource randomSource)
    {
        _config = config;
        _randomSource = randomSource;
        _engine = new GameEngine(_config, _randomSource);

        LoadBestScore();
        LoadGameState();
        UpdateUI();
    }

    [RelayCommand]
    private void NewGame()
    {
        _engine.NewGame();
        UpdateUI();
        SaveGameState();
    }

    [RelayCommand]
    private void Move(string direction)
    {
        if (_engine.CurrentState.IsGameOver)
            return;

        Direction dir = direction switch
        {
            "Up" => Direction.Up,
            "Down" => Direction.Down,
            "Left" => Direction.Left,
            "Right" => Direction.Right,
            _ => throw new ArgumentException($"Invalid direction: {direction}")
        };

        bool moved = _engine.Move(dir);
        
        if (moved)
        {
            UpdateUI();
            SaveGameState();
            
            if (Score > BestScore)
            {
                BestScore = Score;
                SaveBestScore();
            }
        }
    }

    [RelayCommand]
    private void Undo()
    {
        if (_engine.Undo())
        {
            UpdateUI();
            SaveGameState();
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_engine.Redo())
        {
            UpdateUI();
            SaveGameState();
        }
    }

    private void UpdateUI()
    {
        var state = _engine.CurrentState;
        
        Score = state.Score;
        MoveCount = state.MoveCount;
        CanUndo = _engine.CanUndo;
        CanRedo = _engine.CanRedo;

        // Update status text
        if (state.IsGameOver)
        {
            StatusText = "Game Over!";
        }
        else if (state.IsWon)
        {
            StatusText = "You Win!";
        }
        else
        {
            StatusText = string.Empty;
        }

        // Update tiles
        Tiles.Clear();
        for (int row = 0; row < state.Size; row++)
        {
            for (int col = 0; col < state.Size; col++)
            {
                int value = state.GetTile(row, col);
                Tiles.Add(new TileViewModel
                {
                    Row = row,
                    Column = col,
                    Value = value
                });
            }
        }
    }

    private void SaveGameState()
    {
        var dto = _engine.SaveState();
        var json = JsonSerializer.Serialize(dto);
        Preferences.Set("GameState", json);
    }

    private void LoadGameState()
    {
        try
        {
            var json = Preferences.Get("GameState", string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                var dto = JsonSerializer.Deserialize<GameStateDto>(json);
                if (dto != null)
                {
                    _engine.LoadState(dto);
                }
            }
        }
        catch
        {
            // If loading fails, just start a new game
        }
    }

    private void SaveBestScore()
    {
        Preferences.Set("BestScore", BestScore);
    }

    private void LoadBestScore()
    {
        BestScore = Preferences.Get("BestScore", 0);
    }
}

public class TileViewModel
{
    public int Row { get; set; }
    public int Column { get; set; }
    public int Value { get; set; }
}
