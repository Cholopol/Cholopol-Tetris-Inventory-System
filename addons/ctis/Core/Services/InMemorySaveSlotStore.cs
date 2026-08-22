namespace Ctis.Core;

public sealed class InMemorySaveSlotStore : ISaveSlotStore
{
    private readonly string?[] _slots;

    public InMemorySaveSlotStore(int slotCount = InventoryTreeIds.SaveSlotCount)
    {
        SlotCount = Math.Max(1, slotCount);
        _slots = new string?[SlotCount];
    }

    public int SlotCount { get; }

    public bool Exists(int index)
        => IndexInRange(index) && !string.IsNullOrEmpty(_slots[index]);

    public string? Read(int index)
        => IndexInRange(index) ? _slots[index] : null;

    public void Write(int index, string json)
    {
        if (IndexInRange(index))
            _slots[index] = json;
    }

    public void Delete(int index)
    {
        if (IndexInRange(index))
            _slots[index] = null;
    }

    private bool IndexInRange(int index) => index >= 0 && index < SlotCount;
}
