namespace Game2048.Core;

/// <summary>
/// Deterministic implementation of IRandomSource for testing purposes.
/// </summary>
public class SeededRandomSource : IRandomSource
{
    private readonly Random _random;

    public SeededRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    public double NextDouble() => _random.NextDouble();
}
