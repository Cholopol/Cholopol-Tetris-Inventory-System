using Ctis.Core;
using Godot;

namespace Ctis.Presentation;

public static class PlacementConfigLoader
{
    public const string SettingKey = "ctis/placement_config";

    public static string ConfigPath
        => CtisJsonFileStore.PathSetting(SettingKey, "");

    /// <summary>Copies placement rules from JSON when the file exists.</summary>
    public static void LoadInto(PlacementConfig config)
    {
        using var _ = CtisTrace.Scope("PlacementConfig.Load");
        var path = ConfigPath;
        if (string.IsNullOrEmpty(path) || !Godot.FileAccess.FileExists(path))
            return;
        var loaded = CtisJsonFileStore.Read(path, PlacementConfigJson.Parse);
        if (loaded != null)
            config.CopyFrom(loaded);
    }

    /// <summary>Reads placement config, or a default instance when missing or unconfigured.</summary>
    public static PlacementConfig LoadOrDefault()
    {
        using var _ = CtisTrace.Scope("PlacementConfig.Load");
        var path = ConfigPath;
        if (string.IsNullOrEmpty(path) || !Godot.FileAccess.FileExists(path))
            return new PlacementConfig();
        return CtisJsonFileStore.Read(path, PlacementConfigJson.Parse) ?? new PlacementConfig();
    }

    /// <summary>Writes placement rules to JSON.</summary>
    public static Error Save(PlacementConfig config)
    {
        using var _ = CtisTrace.Scope("PlacementConfig.Save");
        var path = ConfigPath;
        if (string.IsNullOrEmpty(path))
            return Error.FileNotFound;
        return CtisJsonFileStore.Write(path, PlacementConfigJson.Serialize(config));
    }
}
