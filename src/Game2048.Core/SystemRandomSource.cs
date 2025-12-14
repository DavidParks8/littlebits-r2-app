namespace Game2048.Core;

/// <summary>
/// Production implementation of IRandomSource using System.Random.
/// </summary>
public class SystemRandomSource : IRandomSource
{
    private readonly Random _random = new();

    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    public double NextDouble() => _random.NextDouble();
}
