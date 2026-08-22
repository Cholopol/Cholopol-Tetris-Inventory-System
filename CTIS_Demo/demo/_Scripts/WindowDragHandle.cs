using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Godot.Views;
using Godot;

namespace Ctis.Demo;

public partial class WindowDragHandle : Control
{
    private Control? _host;
    private Viewport? _capturedGui;
    private bool _dragging;
    private Vector2 _grab;

    public WindowDragHandle()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(0, 36);
        SetProcess(false);
        SetProcessInput(false);
    }

    public override void _EnterTree()
    {
        SetProcessInput(false);
        if (!_dragging)
            SetProcess(false);
        base._EnterTree();
    }

    public override bool _HasPoint(Vector2 point)
    {
        var size = HitSize();
        return point.X >= 0f && point.Y >= 0f && point.X <= size.X && point.Y <= size.Y;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton button
            || button.ButtonIndex != MouseButton.Left
            || !button.Pressed
            || _dragging)
            return;
        if (PointerOnButton() || OccludedByWindowAbove())
            return;
        StartDragging();
        AcceptEvent();
    }

    public override void _Process(double delta)
    {
        if (!_dragging)
            return;
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            StopDragging();
            return;
        }
        ApplyDrag();
    }

    public override void _ExitTree()
    {
        if (_dragging)
            StopDragging();
        base._ExitTree();
    }

    private void StartDragging()
    {
        _host ??= FindHost();
        if (_host == null)
            return;
        CtisTrace.Mark("Window.BeginDrag");
        _dragging = true;
        CaptureGui();
        SetProcess(true);
        _grab = _host.GetGlobalMousePosition() - _host.GlobalPosition;
        _host.MoveToFront();
        ApplyDrag();
    }

    private void ApplyDrag()
    {
        if (_host == null)
            return;
        var viewport = _host.GetViewportRect().Size;
        var next = _host.GetGlobalMousePosition() - _grab;
        next.X = Math.Clamp(next.X, 0, Math.Max(0, viewport.X - _host.Size.X));
        next.Y = Math.Clamp(next.Y, 0, Math.Max(0, viewport.Y - _host.Size.Y));
        if (_host.GlobalPosition.IsEqualApprox(next))
            return;
        _host.GlobalPosition = next;
    }

    private void StopDragging()
    {
        CtisTrace.Mark("Window.EndDrag");
        _dragging = false;
        SetProcess(false);
        ReleaseGui();
    }

    private void CaptureGui()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _capturedGui = _host?.GetViewport() ?? GetViewport();
        if (_capturedGui != null)
            _capturedGui.GuiDisableInput = true;
    }

    private void ReleaseGui()
    {
        MouseFilter = MouseFilterEnum.Stop;
        if (_capturedGui != null && GodotObject.IsInstanceValid(_capturedGui))
            _capturedGui.GuiDisableInput = false;
        _capturedGui = null;
    }

    private Vector2 HitSize()
    {
        var size = Size;
        if (size.X < 1f)
        {
            Control? node = GetParent() as Control;
            while (node != null)
            {
                if (node.Size.X >= 1f)
                {
                    size.X = node.Size.X;
                    break;
                }
                node = node.GetParent() as Control;
            }
        }
        if (size.Y < 1f)
            size.Y = Math.Max(CustomMinimumSize.Y, 36f);
        return size;
    }

    private bool OccludedByWindowAbove()
    {
        var hovered = GetViewport().GuiGetHoveredControl();
        if (hovered == null || hovered == this || IsAncestorOf(hovered))
            return false;
        var hoveredWindow = UiPick.FindAncestor<GodotWindow>(hovered);
        if (hoveredWindow == null || FindHost() is not GodotWindow host)
            return false;
        if (ReferenceEquals(hoveredWindow, host))
            return false;
        if (hoveredWindow.GetParent() == host.GetParent())
            return hoveredWindow.GetIndex() > host.GetIndex();
        return ReferenceEquals(UiPick.TopmostWindowUnderMouse(), hoveredWindow);
    }

    private bool PointerOnButton()
        => PointerOnButton(this, GetGlobalMousePosition());

    private static bool PointerOnButton(Node node, Vector2 mouse)
    {
        if (node is BaseButton button && button.Visible && button.GetGlobalRect().HasPoint(mouse))
            return true;
        int count = node.GetChildCount();
        for (int i = 0; i < count; i++)
        {
            if (PointerOnButton(node.GetChild(i), mouse))
                return true;
        }
        return false;
    }

    private Control? FindHost()
    {
        var node = GetParent();
        while (node != null)
        {
            if (node is GodotWindow window)
                return window;
            node = node.GetParent();
        }
        return GetParent() as Control;
    }
}
