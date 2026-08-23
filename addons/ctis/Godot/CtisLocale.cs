using System.Text;
using Godot;

namespace Ctis.Presentation;

public static class CtisLocale
{
    public const string SettingKey = "ctis/locale";

    public static event Action? Changed;

    private static Translation? _en;
    private static Translation? _zh;
    private static bool _loaded;
    private static readonly List<string> Order = new();
    private static readonly Dictionary<string, LocaleRow> Rows = new();

    public static string CsvPath
    {
        get
        {
            if (ProjectSettings.HasSetting(SettingKey))
            {
                var configured = ProjectSettings.GetSetting(SettingKey).AsString();
                if (!string.IsNullOrEmpty(configured))
                    return configured;
            }
            return "";
        }
    }

    public static bool IsChinese => TranslationServer.GetLocale().StartsWith("zh", StringComparison.OrdinalIgnoreCase);

    /// <summary>Loads locale CSV into Godot TranslationServer.</summary>
    public static void LoadCsv(string? path = null)
    {
        path ??= CsvPath;
        if (_loaded) return;
        EnsureTables();
        if (Godot.FileAccess.FileExists(path))
        {
            var text = Godot.FileAccess.GetFileAsString(path);
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var cols = SplitCsv(lines[i]);
                if (cols.Length < 3 || string.IsNullOrWhiteSpace(cols[0])) continue;
                Upsert(cols[0].Trim(), cols[1], cols[2]);
            }
        }
        else
        {
            RegisterTranslationFile(path.GetBaseName() + ".en.translation");
            RegisterTranslationFile(path.GetBaseName() + ".zh.translation");
        }
        _loaded = true;
    }

    private static void RegisterTranslationFile(string path)
    {
        if (!ResourceLoader.Exists(path)) return;
        var translation = ResourceLoader.Load<Translation>(path);
        if (translation == null) return;
        TranslationServer.AddTranslation(translation);
    }

    /// <summary>Looks up a message, returning the key when missing.</summary>
    public static string Lookup(string key, bool chinese)
    {
        var text = Get(key, chinese);
        return string.IsNullOrEmpty(text) ? key : text;
    }

    /// <summary>Looks up a message, returning empty when missing.</summary>
    public static string Get(string key, bool chinese)
    {
        if (string.IsNullOrEmpty(key) || !Rows.TryGetValue(key, out var row)) return "";
        return chinese ? row.Zh : row.En;
    }

    /// <summary>Creates or updates a locale row.</summary>
    public static void SetMessage(string key, string? en = null, string? zh = null)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        EnsureTables();
        _loaded = true;
        Rows.TryGetValue(key, out var row);
        Upsert(key, en ?? row?.En ?? "", zh ?? row?.Zh ?? "");
    }

    /// <summary>Renames a locale key, merging existing translations.</summary>
    public static void Rename(string from, string to)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || from == to)
            return;
        EnsureTables();
        _loaded = true;
        Rows.TryGetValue(from, out var source);
        Rows.TryGetValue(to, out var dest);
        var en = FirstNonEmpty(source?.En, dest?.En);
        var zh = FirstNonEmpty(source?.Zh, dest?.Zh);
        Rows.Remove(from);
        Order.Remove(from);
        Upsert(to, en, zh);
        _en?.EraseMessage(from);
        _zh?.EraseMessage(from);
    }

    private static string FirstNonEmpty(string? a, string? b)
        => !string.IsNullOrEmpty(a) ? a : b ?? "";

    /// <summary>Deletes a locale row.</summary>
    public static void Remove(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        EnsureTables();
        Rows.Remove(key);
        Order.Remove(key);
        _en?.EraseMessage(key);
        _zh?.EraseMessage(key);
    }

    /// <summary>Drops rows that are neither UI keys (CTIS_*) nor in <paramref name="keys"/>.</summary>
    public static void Retain(IEnumerable<string> keys)
    {
        EnsureTables();
        var keep = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (!string.IsNullOrEmpty(key))
                keep.Add(key);
        }

        var drop = new List<string>();
        for (int i = 0; i < Order.Count; i++)
        {
            var key = Order[i];
            if (!keep.Contains(key) && !key.StartsWith("CTIS_", StringComparison.Ordinal))
                drop.Add(key);
        }

        for (int i = 0; i < drop.Count; i++)
            Remove(drop[i]);
    }

    public static Error SaveCsv(string? path = null)
    {
        path ??= CsvPath;
        if (string.IsNullOrEmpty(path))
            return Error.FileNotFound;
        EnsureTables();
        var dir = ProjectSettings.GlobalizePath(path.GetBaseDir());
        DirAccess.MakeDirRecursiveAbsolute(dir);
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (file == null)
            return Godot.FileAccess.GetOpenError();
        var sb = new StringBuilder();
        sb.AppendLine("keys,en,zh");
        foreach (var key in Order)
        {
            if (!Rows.TryGetValue(key, out var row)) continue;
            sb.Append(Escape(key)).Append(',').Append(Escape(row.En)).Append(',').Append(Escape(row.Zh)).Append('\n');
        }
        file.StoreString(sb.ToString());
        return Error.Ok;
    }

    /// <summary>Switches Godot locale and notifies listeners.</summary>
    public static void SetLocale(string locale)
    {
        TranslationServer.SetLocale(locale);
        Changed?.Invoke();
    }

    private static void EnsureTables()
    {
        if (_en != null && _zh != null) return;
        _en = new Translation { Locale = "en" };
        _zh = new Translation { Locale = "zh" };
        TranslationServer.AddTranslation(_en);
        TranslationServer.AddTranslation(_zh);
    }

    private static void Upsert(string key, string en, string zh)
    {
        if (!Rows.ContainsKey(key))
            Order.Add(key);
        Rows[key] = new LocaleRow { En = en, Zh = zh };
        _en?.AddMessage(key, en);
        _zh?.AddMessage(key, zh);
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string[] SplitCsv(string line)
    {
        var cols = new List<string>();
        var sb = new StringBuilder();
        var quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else quoted = false;
                }
                else sb.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',')
            {
                cols.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        cols.Add(sb.ToString());
        return cols.ToArray();
    }

    private sealed class LocaleRow
    {
        public string En = "";
        public string Zh = "";
    }
}
