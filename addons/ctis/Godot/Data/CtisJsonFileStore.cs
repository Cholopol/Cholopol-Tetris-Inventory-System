using Godot;

namespace Ctis.Presentation;

public static class CtisJsonFileStore
{
    /// <summary>Reads a project-settings path, or <paramref name="fallback"/> when unset.</summary>
    public static string PathSetting(string setting, string fallback)
        => ProjectSettings.HasSetting(setting)
            ? ProjectSettings.GetSetting(setting).AsString()
            : fallback;

    /// <summary>Reads and parses a JSON file; logs and returns default on failure.</summary>
    public static T? Read<T>(string path, Func<string, T> parse)
    {
        if (!Godot.FileAccess.FileExists(path))
            return default;
        try
        {
            return parse(Godot.FileAccess.GetFileAsString(path));
        }
        catch (Exception ex)
        {
            GD.PushError($"[CTIS] Failed to parse {path}: {ex.Message}");
            return default;
        }
    }

    /// <summary>Writes JSON, creating parent folders as needed.</summary>
    public static Error Write(string path, string json)
    {
        var dir = ProjectSettings.GlobalizePath(path.GetBaseDir());
        DirAccess.MakeDirRecursiveAbsolute(dir);
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (file == null)
            return Godot.FileAccess.GetOpenError();
        file.StoreString(json);
        return Error.Ok;
    }
}
