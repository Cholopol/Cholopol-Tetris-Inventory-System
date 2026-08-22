using Ctis.Core;
using DotPudica.Core.Binding;
using Godot;
using AppContext = DotPudica.Godot.AppContext;

namespace Ctis.Presentation;

public sealed class RgbaToColorConverter : IValueConverter<Rgba, Color>
{
    public static readonly RgbaToColorConverter Instance = new();
    public Color Convert(Rgba value) => new(value.R, value.G, value.B, value.A);
    public Rgba ConvertBack(Color value) => new(value.R, value.G, value.B, value.A);
}

public sealed class IconKeyToTextureConverter : IValueConverter<string, Texture2D?>
{
    public static readonly IconKeyToTextureConverter Instance = new();

    public Texture2D? Convert(string value) => ToTexture(value);
    public string ConvertBack(Texture2D? value) => value?.ResourcePath ?? "";

    public static Texture2D? ToTexture(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var atlas = AppContext.Current.Services.GetService(typeof(IIconAtlas)) as IIconAtlas;
        return atlas?.Get(value);
    }
}

public sealed class IntToStackTextConverter : IValueConverter<int, string>
{
    public static readonly IntToStackTextConverter Instance = new();
    public string Convert(int value) => value > 1 ? value.ToString() : "";
    public int ConvertBack(string value) => int.TryParse(value, out var n) ? n : 1;
}

public sealed class Size2ToVector2Converter : IValueConverter<Size2, Vector2>
{
    public static readonly Size2ToVector2Converter Instance = new();
    public Vector2 Convert(Size2 value) => new(value.Width, value.Height);
    public Size2 ConvertBack(Vector2 value) => new(value.X, value.Y);
}

public sealed class BoolToMouseFilterConverter : IValueConverter<bool, Control.MouseFilterEnum>
{
    public static readonly BoolToMouseFilterConverter Instance = new();
    public Control.MouseFilterEnum Convert(bool value)
        => value ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
    public bool ConvertBack(Control.MouseFilterEnum value)
        => value != Control.MouseFilterEnum.Ignore;
}

public sealed class IntToDoubleConverter : IValueConverter<int, double>
{
    public static readonly IntToDoubleConverter Instance = new();
    public double Convert(int value) => value;
    public int ConvertBack(double value) => (int)Math.Round(value);
}

public sealed class FloatToDoubleConverter : IValueConverter<float, double>
{
    public static readonly FloatToDoubleConverter Instance = new();
    public double Convert(float value) => value;
    public float ConvertBack(double value) => (float)value;
}
