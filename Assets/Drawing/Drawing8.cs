using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public static class Blend8
{
    public static byte Lerp(byte a, byte b, byte u)
    {
        return (byte) (a + ((u * (b - a)) >> 8));
    }
}

public class Texture8 : ManagedTexture<byte>
{
    public Texture8(int width, int height)
    {
        this.width = width;
        this.height = height;

        uTexture = Texture2DExtensions.Blank(width, height, TextureFormat.Alpha8);
        uTexture.name = "Texture8";
        pixels = new byte[width * height];
        dirty = true;
    }

    // TODO: this isn't safe, what if the texture is the wrong format
    public Texture8(Texture2D texture)
    {
        width = texture.width;
        height = texture.height;

        uTexture = texture;
        uTexture.name = "Texture8";

        pixels = new byte[width * height];

        SetPixels32(texture.GetPixels32());
    }

    public override void Apply()
    {
        if (dirty)
        {
            uTexture.LoadRawTextureData(pixels);
            uTexture.Apply();
            dirty = false;
        }
    }

    private void SetPixels32(Color32[] pixels)
    {
        for (int i = 0; i < this.pixels.Length; ++i)
        {
            this.pixels[i] = pixels[i].a;
        }

        dirty = true;
    }

    public void DecodeFromPNG(byte[] data)
    {
        var tex = Texture2DExtensions.Blank(1, 1, TextureFormat.Alpha8);
        tex.LoadImage(data);

        SetPixels32(tex.GetPixels32());

        Apply();
    }
}

public static class Brush8
{
    private static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }

    public static ManagedSprite<TPixel> Sweep<TPixel>(ManagedSprite<TPixel> sprite,
                                                      IntVector2 start,
                                                      IntVector2 end,
                                                      Func<int, int, IntVector2, ManagedSprite<TPixel>> GetSprite,
                                                      Blend<TPixel> blend,
                                                      TPixel background = default(TPixel))
    {
        int width  = Mathf.Abs(end.x - start.x) + sprite.rect.width;
        int height = Mathf.Abs(end.y - start.y) + sprite.rect.height;

        var rect = new Rect(0, 0, width, height);

        var sweep = GetSprite(width, height, IntVector2.Zero);
        sweep.Clear(background);

        Sweep(sweep, sprite, start, end, blend);

        return sweep;
    }

    public static ManagedSprite<TPixel> Rectange<TPixel>(int width, int height,
                                                         Func<int, int, IntVector2, ManagedSprite<TPixel>> GetSprite,
                                                         TPixel color,
                                                         IntVector2 pivot =default(IntVector2))
    {
        var rect = GetSprite(width, height, pivot);
        rect.Clear(color);

        return rect;
    }

    public static void Circle<TPixel>(ManagedSprite<TPixel> circle,
                                      int diameter,
                                      TPixel value)
    {
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
                circle.SetPixelAbsolute(i,  y + y0 + yoff, value);
                circle.SetPixelAbsolute(i, -y + y0,        value);
            }

            for (int i = -y + y0; i <= y + y0 + offset; ++i)
            {
                circle.SetPixelAbsolute(i,  x + y0 + xoff, value);
                circle.SetPixelAbsolute(i, -x + y0,        value);
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
                circle.SetPixelAbsolute(i, y0 + 1, value);
            }
        }
    }

    public static void Sweep<TPixel>(ManagedSprite<TPixel> sweep,
                                     ManagedSprite<TPixel> sprite,
                                     IntVector2 start,
                                     IntVector2 end,
                                     Blend<TPixel> blend)
    {
        var tl = new IntVector2(Mathf.Min(start.x, end.x),
                                Mathf.Min(start.y, end.y));

        sweep.pivot = sprite.pivot - tl;

        {
            IntVector2 position;

            int x0 = start.x;
            int y0 = start.y;
            int x1 = end.x;
            int y1 = end.y;

            bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);

            if (steep)   { Swap(ref x0, ref y0); Swap(ref x1, ref y1); }
            if (x0 > x1) { Swap(ref x0, ref x1); Swap(ref y0, ref y1); }

            int dX = (x1 - x0);
            int dY = Mathf.Abs(y1 - y0);

            int err = (dX / 2);
            int ystep = (y0 < y1 ? 1 : -1);
            int y = y0;

            for (int x = x0; x <= x1; ++x)
            {
                if (steep)
                {
                    position.x = y;
                    position.y = x;

                    sweep.Blend(sprite, blend, brushPosition: position);
                }
                else
                {
                    position.x = x;
                    position.y = y;

                    sweep.Blend(sprite, blend, brushPosition: position);
                }

                err = err - dY;

                if (err < 0)
                {
                    y += ystep;
                    err += dX;
                }
            }
        }
    }
}
