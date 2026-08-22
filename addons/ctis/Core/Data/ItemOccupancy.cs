using TetrisCoordLib.Core.Math;
using TetrisCoordLib.Core.Shape;

namespace Ctis.Core;

public sealed class ItemOccupancy
{
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public List<Vec2I> Cells { get; set; } = new();

    /// <summary>Builds a solid rectangle of occupied cells.</summary>
    public static ItemOccupancy Filled(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var cells = new List<Vec2I>(width * height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                cells.Add(new Vec2I(x, y));
        }
        return new ItemOccupancy { Width = width, Height = height, Cells = cells };
    }

    /// <summary>Builds occupancy from an explicit local cell list.</summary>
    public static ItemOccupancy FromCells(int width, int height, params Vec2I[] cells)
        => new()
        {
            Width = Math.Max(1, width),
            Height = Math.Max(1, height),
            Cells = cells.Length == 0 ? new List<Vec2I> { Vec2I.Zero } : new List<Vec2I>(cells)
        };

    /// <summary>Deep-copies width, height, and cells.</summary>
    public ItemOccupancy Clone()
        => new()
        {
            Width = Width,
            Height = Height,
            Cells = Cells != null ? new List<Vec2I>(Cells) : new List<Vec2I>()
        };
}

public sealed class OccupancyPatch
{
    public string Key { get; set; } = "";
    public List<Vec2I> Add { get; set; } = new();
    public List<Vec2I> Remove { get; set; } = new();

    /// <summary>Deep-copies this named add/remove patch.</summary>
    public OccupancyPatch Clone()
        => new()
        {
            Key = Key,
            Add = Add != null ? new List<Vec2I>(Add) : new List<Vec2I>(),
            Remove = Remove != null ? new List<Vec2I>(Remove) : new List<Vec2I>()
        };
}

public static class ItemShape
{
    /// <summary>Applies patches to catalog occupancy and orients into the persisted facing and flips.</summary>
    public static ShapeData Resolve(ItemOccupancy? baseline, IEnumerable<OccupancyPatch>? patches, TetrisItemPersistentData data)
        => Resolve(baseline, patches, data.Direction, data.FlipH, data.FlipV);

    /// <summary>Applies patches to catalog occupancy, then local flips, then facing rotation.</summary>
    public static ShapeData Resolve(
        ItemOccupancy? baseline,
        IEnumerable<OccupancyPatch>? patches,
        Dir dir,
        bool flipH = false,
        bool flipV = false)
    {
        var shape = ToShape(baseline?.Cells);
        if (patches != null)
        {
            foreach (var patch in patches)
            {
                if (patch.Remove is { Count: > 0 })
                    shape = ShapeMorpher.Subtract(shape, ToShape(patch.Remove));
                if (patch.Add is { Count: > 0 })
                    shape = ShapeMorpher.Union(shape, ToShape(patch.Add));
            }
        }
        if (shape.CellCount == 0)
            shape = ShapeData.FromVec2I(new[] { Vec2I.Zero });
        return DirUtil.Orient(shape, dir, flipH, flipV);
    }

    private static ShapeData ToShape(IReadOnlyList<Vec2I>? cells)
    {
        if (cells == null || cells.Count == 0)
            return ShapeData.FromVec2I(Array.Empty<Vec2I>());
        if (cells is Vec2I[] array)
            return ShapeData.FromVec2I(array);
        var direct = new Vec2I[cells.Count];
        for (int i = 0; i < cells.Count; i++)
            direct[i] = cells[i];
        return ShapeData.FromVec2I(direct);
    }

    /// <summary>Clips occupancy cells to a new bounding size.</summary>
    public static ItemOccupancy Resize(ItemOccupancy? occupancy, int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        var source = occupancy?.Cells;
        var cells = new List<Vec2I>(source?.Count ?? 0);
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                var cell = source[i];
                if (cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height)
                    cells.Add(cell);
            }
        }
        if (cells.Count == 0)
            cells.Add(Vec2I.Zero);
        return new ItemOccupancy { Width = width, Height = height, Cells = cells };
    }

    /// <summary>Returns a usable occupancy, filling a 1×1 cell when data is missing.</summary>
    public static ItemOccupancy Ensure(ItemOccupancy? occupancy)
    {
        if (occupancy == null)
            return ItemOccupancy.Filled(1, 1);
        occupancy.Width = Math.Max(1, occupancy.Width);
        occupancy.Height = Math.Max(1, occupancy.Height);
        occupancy.Cells ??= new List<Vec2I>();
        if (occupancy.Cells.Count == 0)
            occupancy.Cells.Add(Vec2I.Zero);
        return occupancy;
    }
}
