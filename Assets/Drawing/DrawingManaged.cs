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

    public void DecodeFromPNG(byte[] data)
    {
        uTexture.LoadImage(data);
        
        pixels = uTexture.GetPixels();
    }

    public override void Apply()
    {
        Apply(force: false);
    }

    public void Apply(bool force=false)
    {
        if (dirty || force)
        {
            uTexture.SetPixels(pixels);
            uTexture.Apply();
            dirty = false;
        }
    }
}

public struct DrawingBrush
{
    public ManagedSprite<Color> sprite;
    public Vector2 position;
    public Blend.Function blend;

    public static ManagedSprite<Color> Circle(int diameter, Color color)
    {
        int left = Mathf.FloorToInt(diameter / 2f);
        float piv = left / (float)diameter;

        Texture2D image = Texture2DExtensions.Blank(diameter, diameter);
        image.Clear(Color.clear);

        Sprite brush = Sprite.Create(image,
                                     new Rect(0, 0, diameter, diameter),
                                     Vector2.one * piv,
                                     1);
        brush.name = "Circle (Brush)";

        int radius = (diameter - 1) / 2;
        int offset = (diameter % 2 == 0) ? 1 : 0;

        int x0 = radius;
        int y0 = radius;

        int x = radius;
        int y = 0;
        int radiusError = 1 - x;

        while (x >= y)
        {
            int yoff = (y > 0 ? 1 : 0) * offset;
            int xoff = (x > 0 ? 1 : 0) * offset;

            for (int i = -x + x0; i <= x + x0 + offset; ++i)
            {
                image.SetPixel(i, y + y0 + yoff, color);
                image.SetPixel(i, -y + y0, color);
            }

            for (int i = -y + y0; i <= y + y0 + offset; ++i)
            {
                image.SetPixel(i, x + y0 + xoff, color);
                image.SetPixel(i, -x + y0, color);
            }

            y++;

            if (radiusError < 0)
            {
                radiusError += 2 * y + 1;
            }
            else
            {
                x--;
                radiusError += 2 * (y - x) + 1;
            }
        }

        if (offset > 0)
        {
            for (int i = 0; i < diameter; ++i)
            {
                image.SetPixel(i, y0 + 1, color);
            }
        }

        var dTexture = new DrawingTexture(image);
        var dSprite = new ManagedSprite<Color>(dTexture, brush);

        return dSprite;
    }

    public static ManagedSprite<Color> Rectangle(int width, int height,
                                          Color color,
                                          float pivotX = 0, float pivotY = 0)
    {
        Texture2D image = Texture2DExtensions.Blank(width, height);
        image.Clear(color);

        Sprite brush = Sprite.Create(image,
                                     new Rect(0, 0, width, height),
                                     new Vector2(pivotX / width, pivotY / height),
                                     1);
        brush.name = "Rectangle (Brush)";

        var dTexture = new DrawingTexture(image);
        var dSprite = new ManagedSprite<Color>(dTexture, brush);

        return dSprite;
    }

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

        int off = thickness % 2 == 0 ? 0 : 1;

        ManagedSprite<Color> circle = Rectangle(thickness, thickness, Color.clear, left, left);
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
