using Godot;

namespace Ctis.Presentation.Editor;

internal static class CtisEditorTheme
{
    public static readonly Color RootBg = Color.FromHtml("#1e1e1e");
    public static readonly Color HeaderBg = Color.FromHtml("#252526");
    public static readonly Color PaneBg = Color.FromHtml("#252526");
    public static readonly Color SectionBg = Color.FromHtml("#2d2d30");
    public static readonly Color FoldHeader = Color.FromHtml("#363636");
    public static readonly Color FieldBg = Color.FromHtml("#3c3c3c");
    public static readonly Color Border = Color.FromHtml("#3c3c3c");
    public static readonly Color BorderStrong = Color.FromHtml("#555555");
    public static readonly Color Text = Color.FromHtml("#e0e0e0");
    public static readonly Color Label = Color.FromHtml("#b0b0b0");
    public static readonly Color Muted = Color.FromHtml("#888888");
    public static readonly Color TabBarBg = Color.FromHtml("#181818");
    public static readonly Color TabActive = Color.FromHtml("#3a3d41");
    public static readonly Color Selected = Color.FromHtml("#264f78");
    public static readonly Color Save = Color.FromHtml("#ff9900");
    public static readonly Color SaveHover = Color.FromHtml("#e68a00");
    public static readonly Color Add = Color.FromHtml("#4caf50");
    public static readonly Color AddHover = Color.FromHtml("#66bb6a");
    public static readonly Color Delete = Color.FromHtml("#f44336");
    public static readonly Color DeleteHover = Color.FromHtml("#ef5350");
    public static readonly Color Status = Color.FromHtml("#007acc");
    public static readonly Color GridCell = Color.FromHtml("#3c3c3c");
    public static readonly Color GridCellActive = Color.FromHtml("#4fc3f7");
    public static readonly Color Accent = Color.FromHtml("#ff9900");
    public const int FontCaption = 15;
    public const int FontBody = 15;
    public const int FontTitle = 16;
    public const int FontHeader = 20;
    public const int FontStatus = 13;

    public static StyleBoxFlat Flat(Color bg, Color? border = null, int radius = 0, int marginX = 8, int marginY = 6)
    {
        var box = new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = marginX,
            ContentMarginRight = marginX,
            ContentMarginTop = marginY,
            ContentMarginBottom = marginY,
            AntiAliasing = false
        };
        if (border is { } b)
        {
            box.BorderColor = b;
            box.SetBorderWidthAll(1);
        }
        return box;
    }

    public static void ApplyButton(Button button, Color bg, Color hover, Color text, int radius = 4, int marginX = 12, int marginY = 6)
    {
        button.AddThemeStyleboxOverride("normal", Flat(bg, radius: radius, marginX: marginX, marginY: marginY));
        button.AddThemeStyleboxOverride("hover", Flat(hover, radius: radius, marginX: marginX, marginY: marginY));
        button.AddThemeStyleboxOverride("pressed", Flat(bg.Darkened(0.15f), radius: radius, marginX: marginX, marginY: marginY));
        button.AddThemeStyleboxOverride("focus", Flat(bg, radius: radius, marginX: marginX, marginY: marginY));
        button.AddThemeColorOverride("font_color", text);
        button.AddThemeColorOverride("font_hover_color", text);
        button.AddThemeColorOverride("font_pressed_color", text);
        button.AddThemeFontSizeOverride("font_size", FontBody);
    }

    public static void ApplyLineEdit(LineEdit edit)
    {
        edit.AddThemeStyleboxOverride("normal", Flat(FieldBg, BorderStrong, 3, 8, 4));
        edit.AddThemeStyleboxOverride("focus", Flat(FieldBg, Selected, 3, 8, 4));
        edit.AddThemeColorOverride("font_color", Colors.White);
        edit.AddThemeColorOverride("caret_color", Colors.White);
        edit.AddThemeFontSizeOverride("font_size", FontBody);
    }

    public static void ApplyOption(OptionButton box, bool compact = false)
    {
        var marginY = compact ? 2 : 4;
        var font = compact ? 13 : FontBody;
        box.AddThemeStyleboxOverride("normal", Flat(FieldBg, BorderStrong, 3, 8, marginY));
        box.AddThemeStyleboxOverride("hover", Flat(FieldBg.Lightened(0.08f), BorderStrong, 3, 8, marginY));
        box.AddThemeStyleboxOverride("pressed", Flat(FieldBg, BorderStrong, 3, 8, marginY));
        box.AddThemeColorOverride("font_color", Colors.White);
        box.AddThemeFontSizeOverride("font_size", font);
    }
}
