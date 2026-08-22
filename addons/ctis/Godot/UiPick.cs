using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Presentation;

public readonly struct UiAncestors
{
    public TetrisSlotView? Slot { get; init; }
    public TetrisItemView? Item { get; init; }
    public TetrisGridView? Grid { get; init; }
    public GodotWindow? Window { get; init; }
}

/// <summary>
/// Picks UI elements strictly within the topmost window under the cursor to prevent click penetration.
/// </summary>
public static class UiPick
{
    public static GodotWindow? TopmostWindowUnderMouse()
    {
        if (AppContext.Current.WindowManager is not Node root)
            return null;
        var viewport = root.GetViewport();
        if (viewport == null)
            return null;
        var mouse = viewport.GetMousePosition();
        var visible = viewport.GetVisibleRect();
        for (int i = root.GetChildCount() - 1; i >= 0; i--)
        {
            if (root.GetChild(i) is GodotWindow window
                && window.IsVisibleInTree() && window.Visible
                && ContainsPointer(window, mouse, visible))
            {
                return window;
            }
        }
        return null;
    }

    public static Control? HitTop(Node? context = null, GodotWindow? topmostWindow = null)
    {
        var node = context ?? AppContext.Current.WindowManager as Node;
        if (node == null || !GodotObject.IsInstanceValid(node))
            return null;

        var viewport = node.GetViewport();
        if (viewport == null)
            return null;

        var hovered = viewport.GuiGetHoveredControl();
        if (hovered == null || !GodotObject.IsInstanceValid(hovered))
            return null;

        var window = topmostWindow ?? TopmostWindowUnderMouse();
        if (window == null)
            return hovered;

        var hoveredWindow = FindAncestor<GodotWindow>(hovered);
        if (hoveredWindow != null && hoveredWindow != window)
            return null;

        return hovered;
    }

    public static T? FindAncestor<T>(Control? control) where T : class
    {
        Node? node = control;
        while (node != null)
        {
            if (node is T match)
                return match;
            node = node.GetParent();
        }
        return null;
    }

    public static UiAncestors ResolveAncestors(Control? control)
    {
        TetrisSlotView? slot = null;
        TetrisItemView? item = null;
        TetrisGridView? grid = null;
        GodotWindow? window = null;
        Node? node = control;
        while (node != null)
        {
            if (slot == null && node is TetrisSlotView slotView)
                slot = slotView;
            if (item == null && node is TetrisItemView itemView)
                item = itemView;
            if (grid == null && node is TetrisGridView gridView)
                grid = gridView;
            if (window == null && node is GodotWindow godotWindow)
                window = godotWindow;
            if (slot != null && item != null && grid != null && window != null)
                break;
            node = node.GetParent();
        }
        return new UiAncestors
        {
            Slot = slot,
            Item = item,
            Grid = grid,
            Window = window
        };
    }

    public static void BringOwningWindowToFront(Control control)
    {
        var window = FindAncestor<GodotWindow>(control);
        if (window == null || window.WindowType == WindowType.Full)
            return;
        window.MoveToFront();
    }

    public static void AddHitBlocker(Control host, Color color)
    {
        host.MouseFilter = Control.MouseFilterEnum.Stop;
        var bg = new ColorRect
        {
            Color = color,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        host.AddChild(bg);
        host.MoveChild(bg, 0);
    }

    public static bool ContainsPointer(Control control)
    {
        if (control is GodotWindow { WindowType: WindowType.Full })
        {
            var viewport = control.GetViewport();
            if (viewport == null)
                return false;
            return viewport.GetVisibleRect().HasPoint(viewport.GetMousePosition());
        }
        var size = control.Size;
        if (size.X < 1f || size.Y < 1f)
            size = control.CustomMinimumSize;
        if (size.X < 1f || size.Y < 1f)
            return false;
        var mouse = control.GetGlobalMousePosition();
        return new Rect2(control.GlobalPosition, size).HasPoint(mouse);
    }

    public static bool ContainsPointer(Control control, Vector2 mouse, Rect2 visibleRect)
    {
        if (control is GodotWindow { WindowType: WindowType.Full })
            return visibleRect.HasPoint(mouse);
        var size = control.Size;
        if (size.X < 1f || size.Y < 1f)
            size = control.CustomMinimumSize;
        if (size.X < 1f || size.Y < 1f)
            return false;
        return new Rect2(control.GlobalPosition, size).HasPoint(mouse);
    }
}
