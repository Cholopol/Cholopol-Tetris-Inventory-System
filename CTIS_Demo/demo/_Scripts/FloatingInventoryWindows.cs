using Ctis.Core;
using Ctis.Presentation;
using DotPudica.Godot.Views;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Demo;

public sealed class FloatingInventoryWindows : IFloatingInventoryWindows
{
    private readonly Dictionary<string, IWindow> _windows = new();
    private readonly LinkedList<string> _focus = new();
    public int MaxGridWindows { get; set; } = 10;
    public int MaxInfoWindows { get; set; } = 5;

    public void ShowItemGrid(TetrisItemVM item)
    {
        CtisTrace.Mark("Window.ShowItemGrid");
        var id = "grid:" + item.Guid;
        if (TryFocus(id)) return;
        Trim(CountWindowsWithPrefix("grid:"), MaxGridWindows, "grid:");
        var wm = AppContext.Current.WindowManager;
        var bundle = new Bundle();
        bundle.Set("item", item);
        bundle.Set("id", id);
        var window = wm.ShowPooled<FloatingGridWindow>(bundle);
        Track(id, window);
    }

    public void ShowItemInfo(ItemDetails? details, int stack)
    {
        CtisTrace.Mark("Window.ShowItemInfo");
        var id = "info:" + (details?.ItemId.ToString() ?? "none");
        if (TryFocus(id)) return;
        Trim(CountWindowsWithPrefix("info:"), MaxInfoWindows, "info:");
        var wm = AppContext.Current.WindowManager;
        var bundle = new Bundle();
        bundle.Set("details", details);
        bundle.Set("stack", stack);
        bundle.Set("id", id);
        var window = wm.ShowPooled<ItemInformationWindow>(bundle);
        Track(id, window);
    }

    public void ShowContextMenu(TetrisItemVM item, Vector2 globalPosition)
    {
        var wm = AppContext.Current.WindowManager;
        foreach (var window in wm.Stack)
        {
            if (window is ContextMenuWindow menu && !menu.Dismissed)
                wm.Dismiss(menu, ignoreAnimation: true);
        }
        var bundle = new Bundle();
        bundle.Set("item", item);
        bundle.Set("pos", globalPosition);
        wm.ShowPooled<ContextMenuWindow>(bundle);
    }

    public void DismissContextMenu()
    {
        var wm = AppContext.Current.WindowManager;
        foreach (var window in wm.Stack)
        {
            if (window is ContextMenuWindow menu && !menu.Dismissed)
                wm.Dismiss(menu, ignoreAnimation: true);
        }
    }

    public void Focus(string uniqueId) => TryFocus(uniqueId);

    public void DismissAll()
    {
        CtisTrace.Mark("Window.DismissAll");
        var wm = AppContext.Current.WindowManager;
        foreach (var window in _windows.Values)
        {
            window.WindowDismissed -= OnTrackedWindowDismissed;
            if (!window.Dismissed)
                wm.Dismiss(window, true);
        }
        _windows.Clear();
        _focus.Clear();
    }

    private bool TryFocus(string id)
    {
        if (!_windows.TryGetValue(id, out var window)) return false;
        if (window.Dismissed)
        {
            Forget(window);
            return false;
        }
        _focus.Remove(id);
        _focus.AddLast(id);
        if (window is Control control)
            control.MoveToFront();
        return true;
    }

    private void Track(string id, IWindow window)
    {
        _windows[id] = window;
        _focus.Remove(id);
        _focus.AddLast(id);
        window.WindowDismissed -= OnTrackedWindowDismissed;
        window.WindowDismissed += OnTrackedWindowDismissed;
    }

    private void OnTrackedWindowDismissed(object? sender, EventArgs e)
    {
        if (sender is IWindow window)
            Forget(window);
    }

    private void Forget(IWindow window)
    {
        window.WindowDismissed -= OnTrackedWindowDismissed;
        string? targetKey = null;
        foreach (var pair in _windows)
        {
            if (ReferenceEquals(pair.Value, window))
            {
                targetKey = pair.Key;
                break;
            }
        }
        if (targetKey != null)
        {
            _windows.Remove(targetKey);
            _focus.Remove(targetKey);
        }
    }

    private int CountWindowsWithPrefix(string prefix)
    {
        int count = 0;
        foreach (var key in _windows.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
                count++;
        }
        return count;
    }

    private void Trim(int count, int max, string prefix)
    {
        while (count >= max && _focus.First != null)
        {
            var oldest = _focus.First.Value;
            _focus.RemoveFirst();
            if (!oldest.StartsWith(prefix, StringComparison.Ordinal)) continue;
            if (_windows.TryGetValue(oldest, out var window) && !window.Dismissed)
                AppContext.Current.WindowManager.Dismiss(window);
            count--;
        }
    }
}
