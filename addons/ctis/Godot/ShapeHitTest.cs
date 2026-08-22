using Ctis.Core;
using Godot;
using TetrisCoordLib.Core.Math;

namespace Ctis.Presentation;

public static class ShapeHitTest
{
    public static bool Contains(IReadOnlyList<Vec2I> cells, int width, int height, Vector2 local, Vector2 size, bool slotRect)
    {
        if (slotRect) return local.X >= 0 && local.Y >= 0 && local.X < size.X && local.Y < size.Y;
        if (width <= 0 || height <= 0 || size.X <= 0 || size.Y <= 0) return false;
        if (local.X < 0 || local.Y < 0 || local.X >= size.X || local.Y >= size.Y) return false;
        int cellX = (int)MathF.Floor(local.X / size.X * width);
        int cellY = (int)MathF.Floor(local.Y / size.Y * height);
        cellX = Math.Clamp(cellX, 0, width - 1);
        cellY = Math.Clamp(cellY, 0, height - 1);
        var point = new Vec2I(cellX, cellY);
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] == point) return true;
        }
        return false;
    }
}
