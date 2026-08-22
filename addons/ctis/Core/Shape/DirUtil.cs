using TetrisCoordLib.Core.Math;
using TetrisCoordLib.Core.Shape;

namespace Ctis.Core;

public static class DirUtil
{
    /// <summary>Maps a facing to 0–3 clockwise quarter turns from Down.</summary>
    public static int ToQuarterTurns(Dir dir) => ((int)dir % 4 + 4) % 4;

    /// <summary>Returns the next clockwise facing.</summary>
    public static Dir Next(Dir dir) => (Dir)((ToQuarterTurns(dir) + 1) % 4);

    /// <summary>True when the facing swaps width and height on screen.</summary>
    public static bool IsRotated(Dir dir) => dir is Dir.Left or Dir.Right;

    private static readonly Dir[][] PreferredSequences = new Dir[][]
    {
        new[] { Dir.Down, Dir.Left, Dir.Up, Dir.Right },
        new[] { Dir.Left, Dir.Down, Dir.Up, Dir.Right },
        new[] { Dir.Up, Dir.Down, Dir.Left, Dir.Right },
        new[] { Dir.Right, Dir.Down, Dir.Left, Dir.Up }
    };

    /// <summary>Returns a non-allocating span with <paramref name="preferred"/> first, then the remaining facings.</summary>
    public static ReadOnlySpan<Dir> PreferThenOthers(Dir preferred)
    {
        int index = ((int)preferred % 4 + 4) % 4;
        return PreferredSequences[index];
    }

    /// <summary>Visual rotation in degrees for the given facing.</summary>
    public static float VisualDegrees(Dir dir) => ToQuarterTurns(dir) * 90f;

    /// <summary>Rotates a local shape into world cells for the given facing.</summary>
    public static ShapeData Rotate(ShapeData baseShape, Dir dir)
        => ShapeTransform.Rotate(baseShape, ToQuarterTurns(dir));

    /// <summary>Flips in local space, then rotates into the given facing.</summary>
    public static ShapeData Orient(ShapeData baseShape, Dir dir, bool flipH = false, bool flipV = false)
    {
        if (flipH)
            baseShape = ShapeTransform.ReflectX(baseShape);
        if (flipV)
            baseShape = ShapeTransform.ReflectY(baseShape);
        return Rotate(baseShape, dir);
    }

    /// <summary>Toggles the local flip flag that produces a screen-axis flip at <paramref name="dir"/>.</summary>
    public static void ToggleVisualFlip(Dir dir, bool horizontal, ref bool flipH, ref bool flipV)
    {
        if (IsRotated(dir) ? !horizontal : horizontal)
            flipH = !flipH;
        else
            flipV = !flipV;
    }
}

public static class Occupancy
{
    /// <summary>Invokes <paramref name="visit"/> for each world cell of a local shape.</summary>
    public static void ForEach(Vec2I origin, IReadOnlyList<Vec2I> cells, Action<int, int> visit)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            var c = origin + cells[i];
            visit(c.X, c.Y);
        }
    }

    /// <summary>Translates local occupancy cells into world coordinates.</summary>
    public static List<Vec2I> Cells(Vec2I origin, IReadOnlyList<Vec2I> local)
    {
        var result = new List<Vec2I>(local.Count);
        for (int i = 0; i < local.Count; i++)
            result.Add(origin + local[i]);
        return result;
    }

    /// <summary>Translates local occupancy cells into a world-cell set.</summary>
    public static HashSet<Vec2I> CellSet(Vec2I origin, IReadOnlyList<Vec2I> local)
    {
        var set = new HashSet<Vec2I>();
        for (int i = 0; i < local.Count; i++)
            set.Add(origin + local[i]);
        return set;
    }
}
