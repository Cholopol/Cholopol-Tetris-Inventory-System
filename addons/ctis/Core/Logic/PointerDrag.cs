namespace Ctis.Core;

/// <summary>Pointer distance used to start a drag; press and current must share the same space.</summary>
public static class PointerDrag
{
    public const float StartThreshold = 4f;

    /// <summary>True when the pointer has moved far enough from the press point to begin a drag.</summary>
    public static bool ExceedsStart(float pressX, float pressY, float currentX, float currentY, float threshold = StartThreshold)
    {
        float dx = currentX - pressX;
        float dy = currentY - pressY;
        return dx * dx + dy * dy > threshold * threshold;
    }
}
