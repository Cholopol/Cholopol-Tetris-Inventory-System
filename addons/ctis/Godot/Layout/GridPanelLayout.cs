using Godot;

namespace Ctis.Presentation;

public readonly record struct GridPlaceholderSpec(int Width, int Height, Vector2 Position, Control? Node = null);

public static class GridPanelLayout
{
    public const string WidthProperty = "width_cells";
    public const string HeightProperty = "height_cells";
    public const string ScriptPath = "res://addons/ctis/Godot/Scenes/GridPanels/grid_placeholder.gd";

    public static Control? Instantiate(string path)
    {
        var resolved = CtisRuntime.ResolveGridPanelScene(path);
        if (string.IsNullOrWhiteSpace(resolved) || !ResourceLoader.Exists(resolved))
            return null;
        return ResourceLoader.Load<PackedScene>(resolved)?.Instantiate<Control>();
    }

    public static IReadOnlyList<GridPlaceholderSpec> Peek(string path)
    {
        var root = Instantiate(path);
        if (root == null)
            return Array.Empty<GridPlaceholderSpec>();
        var collected = Collect(root);
        var specs = new GridPlaceholderSpec[collected.Count];
        for (int i = 0; i < collected.Count; i++)
            specs[i] = collected[i] with { Node = null };
        root.Free();
        return specs;
    }

    public static IReadOnlyList<GridPlaceholderSpec> Collect(Node root)
    {
        var list = new List<GridPlaceholderSpec>();
        foreach (var child in root.GetChildren())
        {
            if (child is not Control control) continue;
            if (!TryReadCells(control, out int width, out int height)) continue;
            list.Add(new GridPlaceholderSpec(width, height, control.Position, control));
        }
        return list;
    }

    public static bool TryReadCells(Control node, out int width, out int height)
    {
        width = 0;
        height = 0;
        var w = node.Get(WidthProperty);
        var h = node.Get(HeightProperty);
        if (w.VariantType == Variant.Type.Nil || h.VariantType == Variant.Type.Nil)
            return false;
        width = Math.Max(1, w.AsInt32());
        height = Math.Max(1, h.AsInt32());
        return true;
    }
}
