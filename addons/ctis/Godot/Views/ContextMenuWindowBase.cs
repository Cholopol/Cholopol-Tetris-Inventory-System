using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Presentation;

/// <summary>
/// Abstract base class for context and popup menus.
/// Employs a full-screen backdrop shield via _GuiInput for zero-GC outside click detection,
/// viewport-clamping, proximity-based auto dismiss, and fade-in animations.
/// </summary>
public abstract partial class ContextMenuWindowBase : GodotWindow
{
    private Tween? _openTween;
    private Control? _activeMenuControl;

    /// <summary>Distance threshold in pixels to automatically dismiss the menu when pointer leaves.</summary>
    public virtual float CloseDistance => 110f;

    /// <summary>Margin preserved between the menu and viewport boundaries.</summary>
    public virtual float ViewportMargin => 4f;

    protected ContextMenuWindowBase()
    {
        WindowType = WindowType.Popup;
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        SetProcess(false);
        SetProcessInput(false);
        SetProcessUnhandledInput(false);
    }

    /// <summary>
    /// Backdrop shield intercepting outside clicks without global input polling or GC allocations.
    /// </summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (!CanAutoClose())
            return;

        if (@event is InputEventMouseButton { Pressed: true }
            || @event is InputEventScreenTouch { Pressed: true })
        {
            CloseMenu();
            AcceptEvent();
        }
    }

    /// <summary>
    /// Dismisses the menu on Escape key press.
    /// </summary>
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!CanAutoClose())
            return;

        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            CloseMenu();
            GetViewport()?.SetInputAsHandled();
        }
    }

    /// <summary>
    /// Prepares menu presentation with viewport-clamping and fade-in animation.
    /// </summary>
    protected void PrepareMenuOpen(Vector2? globalPosition, Control menuControl)
    {
        _activeMenuControl = menuControl;
        SetProcess(true);

        if (globalPosition.HasValue)
        {
            ClampPositionToViewport(globalPosition.Value, menuControl);
        }

        PlayOpenAnimation(menuControl);
    }

    /// <summary>
    /// Clamps menu position within viewport boundaries.
    /// </summary>
    public void ClampPositionToViewport(Vector2 targetPos, Control menuControl)
    {
        var viewport = GetViewport()?.GetVisibleRect();
        var size = menuControl.Size;
        if (size.X < 1f || size.Y < 1f)
            size = menuControl.CustomMinimumSize;

        if (viewport.HasValue)
        {
            var rect = viewport.Value;
            float margin = ViewportMargin;
            float maxX = rect.End.X - size.X - margin;
            float maxY = rect.End.Y - size.Y - margin;
            targetPos.X = Mathf.Clamp(targetPos.X, rect.Position.X + margin, MathF.Max(rect.Position.X + margin, maxX));
            targetPos.Y = Mathf.Clamp(targetPos.Y, rect.Position.Y + margin, MathF.Max(rect.Position.Y + margin, maxY));
        }

        menuControl.GlobalPosition = targetPos;
    }

    /// <summary>
    /// Plays smooth fade-in animation.
    /// </summary>
    protected void PlayOpenAnimation(Control menuControl, float duration = 0.08f)
    {
        _openTween?.Kill();
        menuControl.Modulate = new Color(1f, 1f, 1f, 0f);
        _openTween = CreateTween();
        _openTween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        _openTween.TweenProperty(menuControl, "modulate:a", 1.0f, duration);
    }

    public override void _Process(double delta)
    {
        if (!CanAutoClose() || _activeMenuControl == null || !GodotObject.IsInstanceValid(_activeMenuControl))
            return;

        if (DistanceFromMenu(_activeMenuControl) > CloseDistance)
        {
            CloseMenu();
        }
    }

    protected bool CanAutoClose()
        => IsWindowVisible && !Dismissed && !IsDismissing;

    protected float DistanceFromMenu(Control menuControl)
    {
        var size = menuControl.Size;
        if (size.X < 1f || size.Y < 1f)
            size = menuControl.CustomMinimumSize;
        if (size.X < 1f || size.Y < 1f)
            return 0f;

        var mouse = GetGlobalMousePosition();
        var rect = new Rect2(menuControl.GlobalPosition, size);
        var closest = new Vector2(
            Mathf.Clamp(mouse.X, rect.Position.X, rect.End.X),
            Mathf.Clamp(mouse.Y, rect.Position.Y, rect.End.Y));
        return mouse.DistanceTo(closest);
    }

    /// <summary>
    /// Safely dismisses the menu window and releases animation and input resources.
    /// </summary>
    public void CloseMenu()
    {
        SetProcess(false);
        _openTween?.Kill();
        _openTween = null;
        _activeMenuControl = null;

        if (Dismissed || IsDismissing)
            return;

        AppContext.Current.WindowManager.Dismiss(this, ignoreAnimation: true);
    }

    protected override void OnDismiss()
    {
        SetProcess(false);
        _openTween?.Kill();
        _openTween = null;
        _activeMenuControl = null;
        base.OnDismiss();
    }
}
