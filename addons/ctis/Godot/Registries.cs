using Godot;

namespace Ctis.Presentation;

public interface IIconAtlas
{
    Texture2D? Get(string key);
    void Register(string key, Texture2D texture);
}

public sealed class IconAtlas : IIconAtlas
{
    private readonly Dictionary<string, Texture2D> _map = new();

    public Texture2D? Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (_map.TryGetValue(key, out var tex) && GodotObject.IsInstanceValid(tex))
            return tex;
        var loaded = CtisArt.Load(key);
        if (loaded != null)
            _map[key] = loaded;
        return loaded ?? (key != CtisArt.DefaultIcon ? Get(CtisArt.DefaultIcon) : null);
    }

    public void Register(string key, Texture2D texture) => _map[key] = texture;
}

public sealed class ItemViewRegistry
{
    private readonly Dictionary<string, List<TetrisItemView>> _views = new();

    public void Register(TetrisItemView view)
    {
        var guid = view.BoundViewModel?.Guid;
        if (string.IsNullOrEmpty(guid)) return;
        if (!_views.TryGetValue(guid, out var list))
        {
            list = new List<TetrisItemView>();
            _views[guid] = list;
        }
        if (!list.Contains(view)) list.Add(view);
    }

    public void Unregister(TetrisItemView view)
    {
        var guid = view.BoundViewModel?.Guid;
        if (!string.IsNullOrEmpty(guid) && _views.TryGetValue(guid, out var keyed))
        {
            keyed.Remove(view);
            if (keyed.Count == 0) _views.Remove(guid);
            return;
        }

        var empty = new List<string>();
        foreach (var pair in _views)
        {
            pair.Value.Remove(view);
            if (pair.Value.Count == 0) empty.Add(pair.Key);
        }
        foreach (var key in empty)
            _views.Remove(key);
    }

    public bool TryGetViews(string guid, out List<TetrisItemView> views)
    {
        if (_views.TryGetValue(guid, out var list) && list.Count > 0)
        {
            views = list;
            return true;
        }
        views = null!;
        return false;
    }

    public TetrisItemView? FindUnderParent(string guid, Node parent)
    {
        if (!_views.TryGetValue(guid, out var list)) return null;
        foreach (var view in list)
        {
            if (GodotObject.IsInstanceValid(view) && view.GetParent() == parent)
                return view;
        }
        return null;
    }

    public TetrisItemView? FindActive(string guid)
    {
        if (!_views.TryGetValue(guid, out var list)) return null;
        foreach (var view in list)
        {
            if (GodotObject.IsInstanceValid(view) && view.IsVisibleInTree())
                return view;
        }
        return null;
    }

    public TetrisItemView? HitTestUnderMouse()
        => UiPick.FindAncestor<TetrisItemView>(UiPick.HitTop());
}

public sealed class GridViewRegistry
{
    private readonly Dictionary<string, List<TetrisGridView>> _views = new();

    public void RegisterView(string guid, TetrisGridView view)
    {
        if (string.IsNullOrEmpty(guid) || view == null) return;
        if (!_views.TryGetValue(guid, out var list))
        {
            list = new List<TetrisGridView>();
            _views[guid] = list;
        }
        if (!list.Contains(view)) list.Add(view);
    }

    public void UnregisterView(string guid, TetrisGridView view)
    {
        if (string.IsNullOrEmpty(guid) || !_views.TryGetValue(guid, out var list)) return;
        list.Remove(view);
        if (list.Count == 0) _views.Remove(guid);
    }

    public void UnregisterView(TetrisGridView view)
    {
        var empty = new List<string>();
        foreach (var pair in _views)
        {
            pair.Value.Remove(view);
            if (pair.Value.Count == 0) empty.Add(pair.Key);
        }
        foreach (var key in empty)
            _views.Remove(key);
    }

    public bool TryGetView(string guid, out TetrisGridView view)
    {
        view = null!;
        if (!_views.TryGetValue(guid, out var list)) return false;
        foreach (var candidate in list)
        {
            if (GodotObject.IsInstanceValid(candidate))
            {
                view = candidate;
                return true;
            }
        }
        return false;
    }

    public bool TryGetViews(string guid, out List<TetrisGridView> views)
    {
        if (_views.TryGetValue(guid, out var list) && list.Count > 0)
        {
            views = list;
            return true;
        }
        views = null!;
        return false;
    }
}
