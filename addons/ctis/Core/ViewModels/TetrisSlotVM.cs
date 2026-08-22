using CommunityToolkit.Mvvm.ComponentModel;

namespace Ctis.Core;

public partial class TetrisSlotVM : TetrisItemContainerVM
{
    [ObservableProperty] private InventorySlotType _slotType = InventorySlotType.Pocket;
    [ObservableProperty] private int _slotIndex = -1;
    [ObservableProperty] private Size2 _slotSize = new(CtisSettings.GridTileSizeWidth * 2, CtisSettings.GridTileSizeHeight * 2);
    [ObservableProperty] private string _titleKey = "";

    public event Action<TetrisItemVM>? PlaceItemViewRequested;
    public event Action<TetrisItemVM>? RemoveItemViewRequested;

    public override bool TryPlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0)
    {
        if (RelatedTetrisItem != null && RelatedTetrisItem != tetrisItem) return false;
        PlaceTetrisItem(tetrisItem, 0, 0);
        return true;
    }

    public override void PlaceTetrisItem(TetrisItemVM tetrisItem, int posX = 0, int posY = 0)
    {
        RelatedTetrisItem = tetrisItem;
        tetrisItem.CurrentTetrisContainer = this;
        tetrisItem.UpdateSize(this);
        PlaceItemViewRequested?.Invoke(tetrisItem);
    }

    /// <summary>Clears the equipped item and optionally despawns its view.</summary>
    public void RemoveTetrisItem(bool destroyView)
    {
        var item = RelatedTetrisItem;
        if (item == null) return;
        RelatedTetrisItem = null;
        if (destroyView)
            RemoveItemViewRequested?.Invoke(item);
    }

    /// <summary>Asks the bound view to despawn an item without clearing the slot.</summary>
    public void RequestRemoveItemView(TetrisItemVM item)
        => RemoveItemViewRequested?.Invoke(item);
}
