using Ctis.Core;
using DotPudica.Godot.Views;
using Godot;
using TetrisCoordLib.Core.Math;

namespace Ctis.Presentation;

public interface IPointerGridViews : IPointerGridSession
{
    TetrisGridView? HoveredView { get; }
    void RegisterGrid(TetrisGridView view);
    void UnregisterGrid(TetrisGridView view);
    void RegisterSlot(TetrisSlotView view);
    void UnregisterSlot(TetrisSlotView view);
    void SetHoveredView(TetrisGridView? view);
}

public sealed class PointerGridSession : IPointerGridViews
{
    private readonly List<TetrisGridView> _grids = new();
    private readonly List<TetrisSlotView> _slots = new();
    private Control? _lastHitControl;

    public TetrisGridView? HoveredView { get; private set; }
    public TetrisSlotView? HoveredSlotView { get; private set; }
    public TetrisGridVM? SelectedGrid => HoveredView?.BoundViewModel;
    public TetrisSlotVM? SelectedSlot => HoveredSlotView?.BoundViewModel;
    public TetrisItemVM? HoveredItem { get; private set; }
    public TetrisGridVM? DepositoryGrid { get; set; }
    public bool PreferSlotTarget { get; private set; }

    public void RegisterGrid(TetrisGridView view)
    {
        if (!_grids.Contains(view))
            _grids.Add(view);
    }

    public void UnregisterGrid(TetrisGridView view)
    {
        _grids.Remove(view);
        if (HoveredView == view)
        {
            HoveredView = null;
            _lastHitControl = null;
        }
    }

    public void RegisterSlot(TetrisSlotView view)
    {
        if (!_slots.Contains(view))
            _slots.Add(view);
    }

    public void UnregisterSlot(TetrisSlotView view)
    {
        _slots.Remove(view);
        if (HoveredSlotView == view)
        {
            HoveredSlotView = null;
            _lastHitControl = null;
        }
    }

    public void SetSelectedGrid(TetrisGridVM? grid)
    {
        if (grid == null)
        {
            HoveredView = null;
            _lastHitControl = null;
            return;
        }

        for (int i = 0; i < _grids.Count; i++)
        {
            if (_grids[i].BoundViewModel == grid)
            {
                HoveredView = _grids[i];
                _lastHitControl = null;
                return;
            }
        }
    }

    public void SetHoveredView(TetrisGridView? view)
    {
        HoveredView = view;
        _lastHitControl = null;
    }

    public void RefreshFromMouse()
    {
        if (HoveredView != null && (!GodotObject.IsInstanceValid(HoveredView) || !HoveredView.IsInsideTree()))
        {
            HoveredView = null;
            _lastHitControl = null;
        }
        if (HoveredSlotView != null && (!GodotObject.IsInstanceValid(HoveredSlotView) || !HoveredSlotView.IsInsideTree()))
        {
            HoveredSlotView = null;
            _lastHitControl = null;
        }

        var window = UiPick.TopmostWindowUnderMouse();
        var top = UiPick.HitTop(null, window);
        if (top != null
            && top == _lastHitControl
            && GodotObject.IsInstanceValid(top))
        {
            PreferSlotTarget = HoveredSlotView != null;
            return;
        }

        _lastHitControl = top;
        if (top != null)
        {
            var ancestors = UiPick.ResolveAncestors(top);
            HoveredSlotView = ancestors.Slot;
            HoveredItem = ancestors.Item?.BoundViewModel;
            HoveredView = ancestors.Grid ?? FindRegisteredGridUnderMouse(window);
        }
        else
            ApplyMissHysteresis(window);

        PreferSlotTarget = HoveredSlotView != null;
    }

    private void ApplyMissHysteresis(GodotWindow? window)
    {
        if (HoveredView != null
            && GodotObject.IsInstanceValid(HoveredView)
            && HoveredView.IsInsideTree()
            && HoveredView.IsVisibleInTree()
            && (window == null || HoveredView.OwningWindow == null || HoveredView.OwningWindow == window)
            && HoveredView.ContainsMouse())
            return;

        HoveredSlotView = null;
        HoveredItem = null;
        HoveredView = FindRegisteredGridUnderMouse(window);
    }

    private TetrisGridView? FindRegisteredGridUnderMouse(GodotWindow? activeWindow)
    {
        for (int i = 0; i < _grids.Count; i++)
        {
            var g = _grids[i];
            if (GodotObject.IsInstanceValid(g) && g.IsInsideTree() && g.IsVisibleInTree())
            {
                if (activeWindow != null && g.OwningWindow != null && g.OwningWindow != activeWindow)
                    continue;
                if (g.ContainsMouse())
                    return g;
            }
        }
        return null;
    }

    public Vec2I GetGhostTileGridOrigin(int ghostWidth, int ghostHeight)
    {
        if (HoveredView == null) return Vec2I.Zero;
        var cell = HoveredView.CellUnderMouse();
        return new Vec2I(
            cell.X - (ghostWidth - 1) / 2,
            cell.Y - (ghostHeight - 1) / 2);
    }
}
