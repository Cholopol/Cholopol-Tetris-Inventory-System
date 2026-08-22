using TetrisCoordLib.Core.Math;

namespace Ctis.Core;

public sealed class ItemCatalog : IItemCatalog
{
    private readonly List<ItemDetails> _all = new();
    private readonly Dictionary<int, ItemDetails> _byId = new();

    public IReadOnlyList<ItemDetails> All => _all;
    public int Version { get; set; } = 1;

    /// <summary>Looks up catalog details by item id.</summary>
    public ItemDetails? GetById(int itemId)
        => _byId.TryGetValue(itemId, out var details) ? details : null;

    /// <summary>Registers or replaces a catalog entry.</summary>
    public void Register(ItemDetails details)
    {
        Upsert(details);
        Version++;
    }

    /// <summary>Replaces the entire catalog.</summary>
    public void ReplaceAll(IEnumerable<ItemDetails> details)
    {
        _all.Clear();
        _byId.Clear();
        foreach (var item in details)
            Upsert(item);
        Version++;
    }

    private void Upsert(ItemDetails details)
    {
        _byId[details.ItemId] = details;
        var existing = _all.FindIndex(d => d.ItemId == details.ItemId);
        if (existing >= 0) _all[existing] = details;
        else _all.Add(details);
    }
}

public sealed class NullPointerGridSession : IPointerGridSession
{
    public TetrisGridVM? SelectedGrid { get; private set; }
    public TetrisSlotVM? SelectedSlot { get; private set; }
    public TetrisItemVM? HoveredItem { get; set; }
    public TetrisGridVM? DepositoryGrid { get; set; }
    public bool PreferSlotTarget => SelectedSlot != null && SelectedGrid == null;

    public void SetSelectedGrid(TetrisGridVM? grid) => SelectedGrid = grid;

    public void RefreshFromMouse() { }

    public Vec2I GetGhostTileGridOrigin(int ghostWidth, int ghostHeight)
        => TetrisCoordLib.Core.Math.Vec2I.Zero;
}
