using Godot;
using TetrisCoordLib.Core.Math;
using TetrisCoordLib.Core.Space;
using TetrisCoordLib.Core.Transform;
using TetrisCoordLib.Godot;

namespace Ctis.Presentation;

/// <summary>
/// Provides coordinate space conversions between Screen, Local, Grid, and Shape spaces for Control UI.
/// </summary>
public static class ControlCoordSystem
{
    public static CoordSystem Build(Control grid, float cellWidth, float cellHeight)
    {
        var system = new CoordSystem();
        system.Register(SpaceId.Shape, SpaceId.Grid, XForm2D.Identity);
        system.Register(SpaceId.Grid, SpaceId.Local, XFormFactory.Scale(cellWidth, cellHeight));
        system.Register(SpaceId.Local, SpaceId.Screen, grid.GetGlobalTransformWithCanvas().FromGodotTransform2D());
        return system;
    }

    public static Vec2I ScreenToCell(Control grid, Vector2 screen, float cellWidth, float cellHeight)
    {
        var gridPos = Build(grid, cellWidth, cellHeight)
            .Convert(screen.FromGodotVec2(), SpaceId.Screen, SpaceId.Grid);
        return new Vec2I((int)MathF.Floor(gridPos.X), (int)MathF.Floor(gridPos.Y));
    }

    public static Vec2I LocalToCell(Vector2 local, float cellWidth, float cellHeight)
    {
        if (cellWidth <= 0f || cellHeight <= 0f) return Vec2I.Zero;
        return new Vec2I(
            (int)MathF.Floor(local.X / cellWidth),
            (int)MathF.Floor(local.Y / cellHeight));
    }

    public static Vector2 CanvasMouseToLocal(Control control)
    {
        return control.GetLocalMousePosition();
    }

    public static bool ContainsMouse(Control control, Vector2 size)
    {
        if (size.X < 1f || size.Y < 1f)
            size = control.Size;
        if (size.X < 1f || size.Y < 1f)
            size = control.CustomMinimumSize;
        var local = control.GetLocalMousePosition();
        return local.X >= 0f && local.Y >= 0f && local.X < size.X && local.Y < size.Y;
    }

    public static Vector2 CellToScreen(Control grid, Vec2I cell, float cellWidth, float cellHeight)
        => Build(grid, cellWidth, cellHeight)
            .Convert(cell.ToFloat(), SpaceId.Grid, SpaceId.Screen)
            .ToGodot();
}
