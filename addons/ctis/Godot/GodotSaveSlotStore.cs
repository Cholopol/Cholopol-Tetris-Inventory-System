using Ctis.Core;
using Godot;

namespace Ctis.Presentation;

public sealed class GodotSaveSlotStore : ISaveSlotStore
{
    public int SlotCount => InventoryTreeIds.SaveSlotCount;

    public static string PathFor(int index) => $"user://ctis_save_{index}.json";

    public bool Exists(int index)
        => IndexInRange(index) && Godot.FileAccess.FileExists(PathFor(index));

    public string? Read(int index)
    {
        if (!Exists(index)) return null;
        return Godot.FileAccess.GetFileAsString(PathFor(index));
    }

    public void Write(int index, string json)
    {
        if (!IndexInRange(index)) return;
        using var file = Godot.FileAccess.Open(PathFor(index), Godot.FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushError($"[CTIS] Failed to write save slot {index}: {Godot.FileAccess.GetOpenError()}");
            return;
        }
        file.StoreString(json);
    }

    public void Delete(int index)
    {
        if (!Exists(index)) return;
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(PathFor(index)));
    }

    private static bool IndexInRange(int index) => index >= 0 && index < InventoryTreeIds.SaveSlotCount;
}
