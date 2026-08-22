using Ctis.Core;
using Ctis.Presentation;
using Godot;

namespace Ctis.Demo;

internal static class InventoryChrome
{
    private static readonly PackedScene PlateScene = GD.Load<PackedScene>("res://CTIS_Demo/demo/EmbeddedBagPlate.tscn");

    public static Control WrapEmbeddedBag(string titleKey, Control body, Action<InventorySortStrategy>? onOrganize = null)
    {
        var wrap = PlateScene.Instantiate<PanelContainer>();
        var title = wrap.GetNode<Label>("Margin/Column/Banner/Title");
        title.Text = titleKey;

        if (onOrganize != null)
        {
            var sortBtn = wrap.GetNode<MenuButton>("Margin/Column/Banner/SortBtn");
            sortBtn.Visible = true;
            var popup = sortBtn.GetPopup();
            popup.Clear();
            popup.AddItem("CTIS_SORT_AREA", (int)InventorySortStrategy.Area);
            popup.AddItem("CTIS_SORT_TYPE", (int)InventorySortStrategy.SlotType);
            popup.AddItem("CTIS_SORT_RARITY", (int)InventorySortStrategy.Rarity);
            popup.AddItem("CTIS_SORT_ITEM_ID", (int)InventorySortStrategy.ItemId);
            var bridge = new CtisUi.OrganizeMenuBridge(onOrganize);
            popup.IdPressed += bridge.OnIdPressed;
        }

        var bodyPad = wrap.GetNode<MarginContainer>("Margin/Column/BodyPad");
        bodyPad.AddChild(body);
        return wrap;
    }
}
