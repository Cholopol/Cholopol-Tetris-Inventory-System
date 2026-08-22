namespace Ctis.Core;

public readonly record struct ItemIconRef(string Path, int X, int Y, int Width, int Height)
{
    /// <summary>True when the key includes an atlas region.</summary>
    public bool HasRegion => Width > 0 && Height > 0;

    /// <summary>Parses <c>path</c> or <c>path:x,y,w,h</c> atlas keys.</summary>
    public static ItemIconRef Parse(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return new("", 0, 0, 0, 0);
        var sep = key.LastIndexOf(':');
        if (sep > 0)
        {
            var parts = key[(sep + 1)..].Split(',');
            if (parts.Length == 4
                && int.TryParse(parts[0], out var x)
                && int.TryParse(parts[1], out var y)
                && int.TryParse(parts[2], out var w)
                && int.TryParse(parts[3], out var h)
                && w > 0 && h > 0)
                return new(key[..sep], x, y, w, h);
        }
        return new(key, 0, 0, 0, 0);
    }

    /// <summary>Writes the key back to path or path:region form.</summary>
    public string ToKey()
        => HasRegion ? $"{Path}:{X},{Y},{Width},{Height}" : Path;
}
