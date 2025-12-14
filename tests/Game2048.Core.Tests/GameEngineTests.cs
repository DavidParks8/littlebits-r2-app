using Game2048.Core;

namespace Game2048.Core.Tests;

[TestClass]
public class GameEngineTests
{
    [TestMethod]
    public void NewGame_InitializesWithTwoTiles()
    {
        var config = new GameConfig { Size = 4 };
        var engine = new GameEngine(config, new SeededRandomSource(42));

        var state = engine.CurrentState;
        int nonZeroCount = state.Board.Count(x => x != 0);

        Assert.AreEqual(2, nonZeroCount, "Game should start with exactly 2 tiles");
        Assert.AreEqual(0, state.Score);
        Assert.AreEqual(0, state.MoveCount);
        Assert.IsFalse(state.IsWon);
        Assert.IsFalse(state.IsGameOver);
    }

    [TestMethod]
    public async Task Move_Left_MergesTilesCorrectly_2_2_2_0()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up board manually: [2, 2, 2, 0] in first row
        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.SetTile(0, 1, 2);
        state.SetTile(0, 2, 2);
        state.SetTile(0, 3, 0);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        bool moved = engine.Move(Direction.Left);

        Assert.IsTrue(moved);
        // Expected: [4, 2, 0, 0] + spawned tile somewhere
        Assert.AreEqual(4, engine.CurrentState.GetTile(0, 0));
        Assert.AreEqual(2, engine.CurrentState.GetTile(0, 1));
        Assert.AreEqual(4, engine.CurrentState.Score, "Score should be 4 from one merge");
    }

    [TestMethod]
    public async Task Move_Left_MergesTilesCorrectly_2_2_2_2()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up board: [2, 2, 2, 2]
        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.SetTile(0, 1, 2);
        state.SetTile(0, 2, 2);
        state.SetTile(0, 3, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        bool moved = engine.Move(Direction.Left);

        Assert.IsTrue(moved);
        // Expected: [4, 4, 0, 0] + spawned tile
        Assert.AreEqual(4, engine.CurrentState.GetTile(0, 0));
        Assert.AreEqual(4, engine.CurrentState.GetTile(0, 1));
        Assert.AreEqual(8, engine.CurrentState.Score, "Score should be 8 from two merges");
    }

    [TestMethod]
    public async Task Move_NoOp_DoesNotSpawnTile_DoesNotChangeScore()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up board where left move is no-op: [2, 0, 0, 0]
        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.Score = 10;
        state.MoveCount = 5;
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        int tileCountBefore = engine.CurrentState.Board.Count(x => x != 0);
        int scoreBefore = engine.CurrentState.Score;

        bool moved = engine.Move(Direction.Left);

        int tileCountAfter = engine.CurrentState.Board.Count(x => x != 0);

        Assert.IsFalse(moved, "Move should be no-op");
        Assert.AreEqual(tileCountBefore, tileCountAfter, "No new tile should spawn");
        Assert.AreEqual(scoreBefore, engine.CurrentState.Score, "Score should not change");
        Assert.AreEqual(5, engine.CurrentState.MoveCount, "Move count should not change");
    }

    [TestMethod]
    public async Task Move_Right_CompressesAndMergesCorrectly()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up: [0, 2, 2, 0]
        var state = new GameState(4);
        state.SetTile(0, 1, 2);
        state.SetTile(0, 2, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        bool moved = engine.Move(Direction.Right);

        Assert.IsTrue(moved);
        // Expected: [0, 0, 0, 4] + spawned tile
        Assert.AreEqual(4, engine.CurrentState.GetTile(0, 3));
        Assert.AreEqual(4, engine.CurrentState.Score);
    }

    [TestMethod]
    public async Task Move_Up_CompressesAndMergesCorrectly()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up column 0: [2, 2, 0, 0]
        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.SetTile(1, 0, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        bool moved = engine.Move(Direction.Up);

        Assert.IsTrue(moved);
        // Expected: [4, 0, 0, 0] in column 0 + spawned tile
        Assert.AreEqual(4, engine.CurrentState.GetTile(0, 0));
        Assert.AreEqual(0, engine.CurrentState.GetTile(1, 0));
        Assert.AreEqual(4, engine.CurrentState.Score);
    }

    [TestMethod]
    public async Task Move_Down_CompressesAndMergesCorrectly()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up column 0: [0, 0, 2, 2]
        var state = new GameState(4);
        state.SetTile(2, 0, 2);
        state.SetTile(3, 0, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        bool moved = engine.Move(Direction.Down);

        Assert.IsTrue(moved);
        // Expected: [0, 0, 0, 4] in column 0 + spawned tile
        Assert.AreEqual(0, engine.CurrentState.GetTile(2, 0));
        Assert.AreEqual(4, engine.CurrentState.GetTile(3, 0));
        Assert.AreEqual(4, engine.CurrentState.Score);
    }

    [TestMethod]
    public void SpawnTile_Uses90Percent2And10Percent4()
    {
        // Use a seeded random to verify spawn distribution
        var config = new GameConfig { Size = 4 };
        var counts = new Dictionary<int, int> { { 2, 0 }, { 4, 0 } };

        for (int seed = 0; seed < 100; seed++)
        {
            var random = new SeededRandomSource(seed);
            var engine = new GameEngine(config, random);
            
            // Find the spawned tiles
            foreach (var tile in engine.CurrentState.Board)
            {
                if (tile == 2) counts[2]++;
                else if (tile == 4) counts[4]++;
            }
        }

        // With 200 tiles spawned (2 per game * 100 games), we expect roughly:
        // 180 tiles with value 2 (90%)
        // 20 tiles with value 4 (10%)
        // Allow some variance
        Assert.IsTrue(counts[2] > 160 && counts[2] < 200, $"Expected ~180 '2' tiles, got {counts[2]}");
        Assert.IsTrue(counts[4] > 0 && counts[4] < 40, $"Expected ~20 '4' tiles, got {counts[4]}");
    }

    [TestMethod]
    public async Task WinCondition_DetectsWhenReaching2048()
    {
        var config = new GameConfig { Size = 4, WinTile = 2048 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up board with 1024 tiles that will merge to 2048
        var state = new GameState(4);
        state.SetTile(0, 0, 1024);
        state.SetTile(0, 1, 1024);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        Assert.IsFalse(engine.CurrentState.IsWon);

        engine.Move(Direction.Left);

        Assert.IsTrue(engine.CurrentState.IsWon, "Should detect win when reaching 2048");
    }

    [TestMethod]
    public async Task WinCondition_StopsGameWhenAllowContinueIsFalse()
    {
        var config = new GameConfig 
        { 
            Size = 4, 
            WinTile = 2048,
            AllowContinueAfterWin = false
        };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up board with 1024 tiles
        var state = new GameState(4);
        state.SetTile(0, 0, 1024);
        state.SetTile(0, 1, 1024);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        engine.Move(Direction.Left);

        Assert.IsTrue(engine.CurrentState.IsWon);
        Assert.IsTrue(engine.CurrentState.IsGameOver, "Game should end when win tile reached and AllowContinueAfterWin is false");
    }

    [TestMethod]
    public async Task GameOver_DetectsWhenNoMovesAvailable()
    {
        var config = new GameConfig { Size = 2 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up a 2x2 board with no possible merges:
        // [2, 4]
        // [4, 2]
        var state = new GameState(2);
        state.SetTile(0, 0, 2);
        state.SetTile(0, 1, 4);
        state.SetTile(1, 0, 4);
        state.SetTile(1, 1, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        // Try a move - should be no-op but trigger game over check
        engine.Move(Direction.Left);

        Assert.IsTrue(engine.CurrentState.IsGameOver, "Game should be over when board is full and no merges possible");
    }

    [TestMethod]
    public async Task Undo_RestoresPreviousState()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        // Set up a simple board
        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.SetTile(0, 1, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        int scoreBefore = engine.CurrentState.Score;
        var boardBefore = (int[])engine.CurrentState.Board.Clone();

        engine.Move(Direction.Left);

        Assert.IsTrue(engine.CanUndo);

        bool undone = engine.Undo();

        Assert.IsTrue(undone);
        Assert.AreEqual(scoreBefore, engine.CurrentState.Score);
        CollectionAssert.AreEqual(boardBefore, engine.CurrentState.Board);
    }

    [TestMethod]
    public async Task Undo_DoesNotSpawnNewTile()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.SetTile(0, 1, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        engine.Move(Direction.Left);

        int tileCountAfterMove = engine.CurrentState.Board.Count(x => x != 0);

        engine.Undo();

        int tileCountAfterUndo = engine.CurrentState.Board.Count(x => x != 0);

        Assert.AreEqual(2, tileCountAfterUndo, "Undo should restore exact previous state without spawning");
    }

    [TestMethod]
    public async Task Redo_RestoresNextState()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.SetTile(0, 1, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        engine.Move(Direction.Left);

        var boardAfterMove = (int[])engine.CurrentState.Board.Clone();
        int scoreAfterMove = engine.CurrentState.Score;

        engine.Undo();
        Assert.IsTrue(engine.CanRedo);

        bool redone = engine.Redo();

        Assert.IsTrue(redone);
        Assert.AreEqual(scoreAfterMove, engine.CurrentState.Score);
        CollectionAssert.AreEqual(boardAfterMove, engine.CurrentState.Board);
    }

    [TestMethod]
    public async Task Redo_ClearsOnNewMove()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.SetTile(0, 1, 2);
        state.SetTile(1, 0, 4);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        engine.Move(Direction.Left);
        engine.Undo();

        Assert.IsTrue(engine.CanRedo);

        engine.Move(Direction.Down);

        Assert.IsFalse(engine.CanRedo, "Redo stack should be cleared after a new move");
    }

    [TestMethod]
    public async Task Serialization_SaveAndLoadState()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.SetTile(0, 1, 4);
        state.SetTile(1, 0, 8);
        state.Score = 100;
        state.MoveCount = 10;
        state.IsWon = true;
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        var dto = await engine.SaveStateAsync();

        var newEngine = new GameEngine(config, random);
        await newEngine.LoadStateAsync(dto);

        Assert.AreEqual(engine.CurrentState.Size, newEngine.CurrentState.Size);
        Assert.AreEqual(engine.CurrentState.Score, newEngine.CurrentState.Score);
        Assert.AreEqual(engine.CurrentState.MoveCount, newEngine.CurrentState.MoveCount);
        Assert.AreEqual(engine.CurrentState.IsWon, newEngine.CurrentState.IsWon);
        CollectionAssert.AreEqual(engine.CurrentState.Board, newEngine.CurrentState.Board);
    }

    [TestMethod]
    public async Task UndoStack_RespectsBoundedCapacity()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        state.SetTile(0, 1, 2);
        state.SetTile(1, 0, 2);
        state.SetTile(1, 1, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        // Make 51 moves (more than max history of 50)
        for (int i = 0; i < 51; i++)
        {
            engine.Move(i % 2 == 0 ? Direction.Left : Direction.Right);
        }

        // Count how many times we can undo
        int undoCount = 0;
        while (engine.CanUndo)
        {
            engine.Undo();
            undoCount++;
        }

        Assert.IsTrue(undoCount <= 50, $"Should not be able to undo more than 50 times, got {undoCount}");
    }

    [TestMethod]
    public async Task MoveCount_IncrementsOnlyOnValidMoves()
    {
        var config = new GameConfig { Size = 4 };
        var random = new SeededRandomSource(42);
        var engine = new GameEngine(config, random);

        var state = new GameState(4);
        state.SetTile(0, 0, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));

        int moveCountBefore = engine.CurrentState.MoveCount;

        // Try a no-op move
        engine.Move(Direction.Left);

        Assert.AreEqual(moveCountBefore, engine.CurrentState.MoveCount, "No-op move should not increment move count");

        // Try a valid move
        state.SetTile(0, 1, 2);
        await engine.LoadStateAsync(GameStateDto.FromGameState(state));
        moveCountBefore = engine.CurrentState.MoveCount;

        engine.Move(Direction.Left);

        Assert.AreEqual(moveCountBefore + 1, engine.CurrentState.MoveCount, "Valid move should increment move count");
    }
}
