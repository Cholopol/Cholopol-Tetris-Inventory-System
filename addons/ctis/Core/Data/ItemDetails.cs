using System.Text.Json.Serialization;

namespace Ctis.Core;

public sealed class ItemDetails
{
    public int ItemId { get; set; }
    public string NameKey { get; set; } = "";
    public string DescriptionKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string IconKey { get; set; } = "";
    public InventorySlotType SlotType { get; set; }
    public ItemRarity Rarity { get; set; }
    public int MaxStack { get; set; }
    public ItemOccupancy Occupancy { get; set; } = ItemOccupancy.Filled(1, 1);
    public Dir DefaultDirection { get; set; } = Dir.Down;
    public string GridPanelSceneKey { get; set; } = "";
    public int ItemDamage { get; set; }
    public float Weight { get; set; }
    public int ItemPrice { get; set; }

    /// <summary>Locale key for the item name, falling back to <see cref="DisplayName"/>.</summary>
    [JsonIgnore]
    public string NameText => string.IsNullOrWhiteSpace(NameKey) ? DisplayName : NameKey;

    /// <summary>True when this item owns inner grids via a panel scene.</summary>
    [JsonIgnore]
    public bool HasInnerGrid => !string.IsNullOrWhiteSpace(GridPanelSceneKey);
}
