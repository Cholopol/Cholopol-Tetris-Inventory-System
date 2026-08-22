namespace Ctis.Core;

public enum EquipmentSlotGroup
{
    Character,
    Weapon,
    Container
}

public sealed class EquipmentSlotSpec
{
    public InventorySlotType SlotType { get; set; }
    public int SlotIndex { get; set; }
    public int CellsWidth { get; set; } = 2;
    public int CellsHeight { get; set; } = 2;
    public EquipmentSlotGroup Group { get; set; }
    public string TitleKey { get; set; } = "";
}

public sealed class EquipmentLayout
{
    public IReadOnlyList<EquipmentSlotSpec> Slots { get; private set; } = Array.Empty<EquipmentSlotSpec>();

    /// <summary>Slots in a single equipment group.</summary>
    public IReadOnlyList<EquipmentSlotSpec> OfGroup(EquipmentSlotGroup group)
    {
        var result = new List<EquipmentSlotSpec>();
        for (int i = 0; i < Slots.Count; i++)
        {
            var slot = Slots[i];
            if (slot.Group == group)
                result.Add(slot);
        }
        return result;
    }

    /// <summary>Looks up a slot by its unique index.</summary>
    public EquipmentSlotSpec? Find(int slotIndex)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].SlotIndex == slotIndex)
                return Slots[i];
        }
        return null;
    }

    /// <summary>Replaces the layout, throwing when indexes or sizes are invalid.</summary>
    public void ReplaceAll(IEnumerable<EquipmentSlotSpec> slots)
    {
        if (!TryReplaceAll(slots, out var error))
            throw new ArgumentException(error);
    }

    /// <summary>Replaces the layout; false when indexes collide or sizes are invalid.</summary>
    public bool TryReplaceAll(IEnumerable<EquipmentSlotSpec> slots, out string? error)
    {
        var list = slots as IReadOnlyList<EquipmentSlotSpec> ?? new List<EquipmentSlotSpec>(slots);
        error = DescribeProblem(list);
        if (error != null)
            return false;
        Slots = list;
        return true;
    }

    private static string? DescribeProblem(IReadOnlyList<EquipmentSlotSpec> slots)
    {
        var indexes = new HashSet<int>();
        var containerTypes = new HashSet<InventorySlotType>();
        foreach (var slot in slots)
        {
            if (slot.CellsWidth < 1 || slot.CellsHeight < 1)
                return $"Slot {slot.SlotIndex} size must be at least 1x1.";
            if (!indexes.Add(slot.SlotIndex))
                return $"Duplicate slot index {slot.SlotIndex}.";
            if (slot.Group == EquipmentSlotGroup.Container && !containerTypes.Add(slot.SlotType))
                return $"Duplicate container slot type {slot.SlotType}.";
        }
        return null;
    }
}
