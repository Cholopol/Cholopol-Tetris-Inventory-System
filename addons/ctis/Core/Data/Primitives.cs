namespace Ctis.Core;

public readonly record struct Rgba(float R, float G, float B, float A)
{
    public static Rgba White => new(1f, 1f, 1f, 1f);
    public static Rgba Clear => new(0f, 0f, 0f, 0f);

    public Rgba WithAlpha(float a) => this with { A = a };

    public Rgba Darken(float factor)
    {
        factor = Math.Clamp(factor, 0f, 1f);
        return new Rgba(
            R + (0f - R) * factor,
            G + (0f - G) * factor,
            B + (0f - B) * factor,
            A);
    }
}

public readonly record struct Size2(float Width, float Height);
