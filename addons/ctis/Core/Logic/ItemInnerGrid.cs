namespace Ctis.Core;

public readonly record struct InnerGridSpec(int Index, int Width, int Height);

public static class ItemInnerGrid
{
    /// <summary>Builds the inner-container id <c>{itemGuid}:{index}</c>.</summary>
    public static string ContainerId(string itemGuid, int index) => itemGuid + ":" + index;

    /// <summary>Creates or reuses an item-owned grid and applies resolved size.</summary>
    public static TetrisGridVM Configure(
        TetrisItemVM item,
        int index,
        IInventoryTreeCache tree,
        IGridFactory grids,
        IInventoryService inventory,
        int width = 0,
        int height = 0)
    {
        var grid = item.GetOrCreateGridVM(index, grids);
        ResolveSize(item.ItemDetails, index, width, height, tree, grid.GridGuid, out int w, out int h, out float tileW, out float tileH);
        inventory.Apply(InventoryCommand.ResizeContainer(grid.GridGuid, w, h, tileW, tileH));
        tree.SetContainerOwner(grid.GridGuid, item.Guid);
        grid.ApplyConfig(w, h, tileW, tileH);
        return grid;
    }

    /// <summary>Ensures every inner grid from <paramref name="specs"/> exists on Tree without opening a window.</summary>
    public static IReadOnlyList<TetrisGridVM> EnsureAll(
        TetrisItemVM item,
        IReadOnlyList<InnerGridSpec> specs,
        IInventoryTreeCache tree,
        IGridFactory grids,
        IInventoryService inventory)
    {
        if (specs.Count == 0) return Array.Empty<TetrisGridVM>();
        var ordered = new List<InnerGridSpec>(specs);
        ordered.Sort(static (a, b) => a.Index.CompareTo(b.Index));
        var result = new List<TetrisGridVM>(ordered.Count);
        for (int i = 0; i < ordered.Count; i++)
            result.Add(Configure(item, ordered[i].Index, tree, grids, inventory, ordered[i].Width, ordered[i].Height));
        return result;
    }

    /// <summary>Resolves inner-grid size from explicit placeholder args, then saved tree config.</summary>
    public static void ResolveSize(
        ItemDetails? details,
        int index,
        int width,
        int height,
        IInventoryTreeCache tree,
        string gridGuid,
        out int resolvedWidth,
        out int resolvedHeight,
        out float tileWidth,
        out float tileHeight)
    {
        tileWidth = CtisSettings.GridTileSizeWidth;
        tileHeight = CtisSettings.GridTileSizeHeight;

        if (width > 0 && height > 0)
        {
            resolvedWidth = width;
            resolvedHeight = height;
            return;
        }

        if (tree.TryGetContainer(gridGuid, out var node)
            && (node.GridSizeWidth > 1 || node.GridSizeHeight > 1))
        {
            resolvedWidth = node.GridSizeWidth;
            resolvedHeight = node.GridSizeHeight;
            tileWidth = node.LocalGridTileSizeWidth;
            tileHeight = node.LocalGridTileSizeHeight;
            return;
        }

        throw new InvalidOperationException(
            $"Inner grid size is missing for item {details?.ItemId ?? 0} index {index}.");
    }
}
