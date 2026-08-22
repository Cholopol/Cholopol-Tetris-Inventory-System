using Ctis.Core;
using Godot;

namespace Ctis.Presentation;

public static class EquipmentLayoutLoader
{
    public const string SettingKey = "ctis/equipment_layout";

    public static string LayoutPath
        => CtisJsonFileStore.PathSetting(SettingKey, "");

    /// <summary>Loads equipment slots from JSON, returning an empty list when unconfigured or missing.</summary>
    public static List<EquipmentSlotSpec> LoadOrDefault()
    {
        using var _ = CtisTrace.Scope("EquipmentLayout.Load");
        var path = LayoutPath;
        if (string.IsNullOrEmpty(path) || !Godot.FileAccess.FileExists(path))
            return new List<EquipmentSlotSpec>();
        return CtisJsonFileStore.Read(path, EquipmentLayoutJson.Parse) ?? new List<EquipmentSlotSpec>();
    }

    /// <summary>Loads equipment slots from JSON when the file is valid.</summary>
    public static void LoadInto(EquipmentLayout layout)
    {
        using var _ = CtisTrace.Scope("EquipmentLayout.Load");
        var path = LayoutPath;
        if (string.IsNullOrEmpty(path) || !Godot.FileAccess.FileExists(path))
            return;
        var slots = CtisJsonFileStore.Read(path, EquipmentLayoutJson.Parse);
        if (slots is not { Count: > 0 })
            return;
        if (!layout.TryReplaceAll(slots, out var error))
            GD.PushError($"[CTIS] Invalid equipment layout {path}: {error}");
    }

    /// <summary>Writes the current equipment layout to JSON.</summary>
    public static Error Save(EquipmentLayout layout) => Save(layout.Slots);

    /// <summary>Writes equipment slots to JSON.</summary>
    public static Error Save(IReadOnlyList<EquipmentSlotSpec> slots)
    {
        using var _ = CtisTrace.Scope("EquipmentLayout.Save");
        var path = LayoutPath;
        if (string.IsNullOrEmpty(path))
            return Error.FileNotFound;
        return CtisJsonFileStore.Write(path, EquipmentLayoutJson.Serialize(slots));
    }
}
