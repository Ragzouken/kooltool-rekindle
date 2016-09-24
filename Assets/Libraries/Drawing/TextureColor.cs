using UnityEngine;

public class TextureColor : ManagedTexture<Color>
{
    public class Pooler : ManagedPooler<Pooler, Color>
    {
        public override ManagedTexture<Color> CreateTexture(int width, int height)
        {
            return new TextureColor(width, height);
        }
    }

    public static Color Lerp(Color a, Color b, float u)
    {
        a.a = a.a * (1 - u) + b.a * u;
        a.r = a.r * (1 - u) + b.r * u;
        a.g = a.g * (1 - u) + b.g * u;
        a.b = a.b * (1 - u) + b.b * u;

        return a;
    }

    public static Blend<Color> mask     = (canvas, brush) => brush.a > 0 ? brush : canvas;
    public static Blend<Color> alpha    = (canvas, brush) => Lerp(canvas, brush, brush.a);
    public static Blend<Color> add      = (canvas, brush) => canvas + brush;
    public static Blend<Color> subtract = (canvas, brush) => canvas - brush;
    public static Blend<Color> multiply = (canvas, brush) => canvas * brush;
    public static Blend<Color> replace  = (canvas, brush) => brush;

    public static Blend<Color> stencilKeep = (canvas, brush) => Lerp(Color.clear, canvas, brush.a);
    public static Blend<Color> stencilCut  = (canvas, brush) => Lerp(canvas, Color.clear, brush.a);

    public TextureColor(int width, int height)
        : base(width, height, TextureFormat.ARGB32)
    {
    }

    public override void Apply()
    {
        if (dirty)
        {
            uTexture.SetPixels(pixels);
            uTexture.Apply();
            dirty = false;
        }
    }
}
