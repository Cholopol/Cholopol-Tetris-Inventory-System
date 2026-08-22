using Ctis.Core;
using Godot;

namespace Ctis.Presentation;

public sealed class SceneInnerGridLayout : IInnerGridLayout
{
    private readonly Dictionary<string, IReadOnlyList<InnerGridSpec>> _cache = new(StringComparer.Ordinal);

    /// <summary>Reads inner-grid sizes from the item panel scene, cached by path.</summary>
    public IReadOnlyList<InnerGridSpec> SpecsFor(ItemDetails? details)
    {
        var path = details?.GridPanelSceneKey;
        if (string.IsNullOrWhiteSpace(path))
            return Array.Empty<InnerGridSpec>();
        if (_cache.TryGetValue(path, out var cached))
            return cached;

        var peek = GridPanelLayout.Peek(path);
        var specs = new InnerGridSpec[peek.Count];
        for (int i = 0; i < peek.Count; i++)
            specs[i] = new InnerGridSpec(i, peek[i].Width, peek[i].Height);
        _cache[path] = specs;
        return specs;
    }
}
