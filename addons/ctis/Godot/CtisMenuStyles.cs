using Godot;

namespace Ctis.Presentation;

/// <summary>
/// Menu specifications and static theme resource path definitions.
/// Visual styling is driven by static Godot Themes with built-in fallbacks.
/// </summary>
public static class CtisMenuStyles
{
    public const string BuiltinMenuThemePath = "res://addons/ctis/Art/Theme/ctis_menu_theme.tres";
    public const string MenuThemePath = BuiltinMenuThemePath;

    public const int MenuItemFontSize = 11;
    public const int HeaderFontSize = 10;
    public const float MenuItemHeight = 22f;
    public const float HeaderHeight = 18f;
    public const float ItemPaddingHorizontal = 6f;
    public const float ItemPaddingVertical = 2f;

    /// <summary>
    /// Resolves active menu theme path from ProjectSettings or built-in fallback.
    /// </summary>
    public static string GetActiveMenuThemePath()
    {
        if (ProjectSettings.HasSetting("ctis/menu_theme"))
        {
            var configured = (string)ProjectSettings.GetSetting("ctis/menu_theme");
            if (!string.IsNullOrEmpty(configured) && ResourceLoader.Exists(configured))
                return configured;
        }
        return BuiltinMenuThemePath;
    }
}
