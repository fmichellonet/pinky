namespace Pinky;

public static class GhostExtensions
{
    /// <summary>Set a return value for this call.</summary>
    /// <param name="value"></param>
    /// <param name="returnThis">Value to return</param>
    public static void Returns<T>(this T value, T returnThis) { }
}