using System.Text;

namespace Ctis.Core;

public static class ItemLocaleKeys
{
    public const string NamePrefix = "ITEMS_";
    public const string DescPrefix = "DESC_";

    /// <summary>Builds the display-name locale key for a token.</summary>
    public static string Name(string token) => NamePrefix + token;

    /// <summary>Builds the description locale key for a token.</summary>
    public static string Desc(string token) => DescPrefix + token;

    /// <summary>Normalizes a display name into an uppercase locale token.</summary>
    public static string Token(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var sb = new StringBuilder(name.Length);
        var pending = false;
        foreach (var c in name.Trim())
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                if (pending && sb.Length > 0)
                    sb.Append('_');
                pending = false;
                sb.Append(char.ToUpperInvariant(c));
            }
            else
                pending = true;
        }
        return sb.ToString();
    }

    /// <summary>Strips ITEMS_/DESC_ prefixes from a locale key, or tokenizes a raw name.</summary>
    public static string TokenFromKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (key.StartsWith(NamePrefix, StringComparison.Ordinal))
            return key[NamePrefix.Length..];
        if (key.StartsWith(DescPrefix, StringComparison.Ordinal))
            return key[DescPrefix.Length..];
        return Token(key);
    }

    /// <summary>Tokenizes a display name and appends _2, _3… when the token is already taken.</summary>
    public static string UniqueToken(string name, Func<string, bool> taken)
    {
        var token = Token(name);
        if (string.IsNullOrEmpty(token)) return "";
        if (!taken(token)) return token;
        for (int n = 2; ; n++)
        {
            var candidate = $"{token}_{n}";
            if (!taken(candidate)) return candidate;
        }
    }
}
