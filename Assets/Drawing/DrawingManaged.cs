using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DrawingTexture : ManagedTexture<Color>
{
    public DrawingTexture(Texture2D texture)
    {
        width = texture.width;
        height = texture.height;
        uTexture = texture;

        pixels = texture.GetPixels();
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

public struct DrawingBrush
{
    public static ManagedSprite<Color> Line(Vector2 start,
                                     Vector2 end,
                                     Color color,
                                     int thickness)
    {
        var tl = new Vector2(Mathf.Min(start.x, end.x),
                             Mathf.Min(start.y, end.y));

        int left = Mathf.FloorToInt(thickness / 2f);

        Vector2 size = new Vector2(Mathf.Abs(end.x - start.x) + thickness,
                                   Mathf.Abs(end.y - start.y) + thickness);

        var pivot = tl * -1 + Vector2.one * left;
        var rect = new Rect(0, 0, size.x, size.y);

        var dTexture = DrawingTexturePooler.GetTexture((int) size.x, (int) size.y);
        var dSprite = new ManagedSprite<Color>(dTexture, rect, pivot);
        dSprite.Clear(Color.clear);

        var circle = DrawingTexturePooler.GetSprite(thickness, thickness, Vector2.one * left);
        circle.Clear(Color.clear);
        Brush8.Circle<Color>(circle, thickness, color);
        {
            Blend<Color> alpha = (canvas, brush) => Blend.Lerp(canvas, brush, brush.a);

            Bresenham.PlotFunction plot = delegate (int x, int y)
            {
                dSprite.Blend(circle, alpha, brushPosition: new Vector2(x, y));
            };

            Bresenham.Line((int)start.x,
                           (int)start.y,
                           (int)end.x,
                           (int)end.y,
                           plot);
        }

        return dSprite;
    }
}

public static class DrawingTexturePooler
{
    private static Stack<DrawingTexture> textures = new Stack<DrawingTexture>();

    public static DrawingTexture GetTexture(int width, int height)
    {
        DrawingTexture dTexture;

        if (textures.Count > 0)
        {
            dTexture = textures.Pop();
        }
        else
        {
            var tex = Texture2DExtensions.Blank(256, 256);

            dTexture = new DrawingTexture(tex);
        }

        return dTexture;
    }

    public static ManagedSprite<Color> GetSprite(int width, int height, Vector2 pivot = default(Vector2))
    {
        var texture = GetTexture(width, height);

        return new ManagedSprite<Color>(texture, new Rect(0, 0, width, height), pivot);
    }

    public static void FreeTexture(DrawingTexture texture)
    {
        textures.Push(texture);
    }
}
