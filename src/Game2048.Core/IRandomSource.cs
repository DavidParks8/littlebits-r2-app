namespace Game2048.Core;

/// <summary>
/// Abstraction for random number generation to enable deterministic testing.
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Returns a non-negative random integer that is less than the specified maximum.
    /// </summary>
    int Next(int maxExclusive);

    /// <summary>
    /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
    /// </summary>
    double NextDouble();
}
