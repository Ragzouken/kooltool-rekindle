using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public static class Blend8
{
    public struct Data
    {
        public byte canvas;
        public byte brush;
    }

    public delegate byte Function(Data data);

    public static byte Lerp(byte a, byte b, byte u)
    {
        return (byte) (a + ((u * (b - a)) >> 8));
    }

    public static Function mask     = data => data.brush > 0 ? data.brush : data.canvas;
    public static Function alpha    = mask;
    //public static Function add      = data => data.canvas + data.brush;
    //public static Function subtract = data => data.canvas - data.brush;
    //public static Function multiply = data => data.canvas * data.brush;
    public static Function replace  = data => data.brush;
}

public class Texture8 : ManagedTexture<byte>
{
    //public Blend<byte> mask = (canvas, brush) => brush == 0 ? canvas : canvas;

    public Texture8(int width, int height, byte value=0)
    {
        uTexture = Texture2DExtensions.Blank(width, height, TextureFormat.Alpha8);
        pixels = new byte[width * height];
     
        for (int i = 0; i < pixels.Length; ++i)
        {
            pixels[i] = value;
        }

        dirty = true;
    }

    public Texture8(Texture2D texture)
    {
        this.uTexture = texture;
        texture.name = "Texture8";

        pixels = new byte[texture.width * texture.height];

        SetPixels32(texture.GetPixels32());
    }

    public override void Apply()
    {
        Apply(force: false);
    }

    public void Apply(bool force=false)
    {
        if (dirty || force)
        {
            uTexture.LoadRawTextureData(pixels);
            uTexture.Apply();
            dirty = false;
        }
    }

    public void SetPixels32(Color32[] pixels)
    {
        for (int i = 0; i < this.pixels.Length; ++i)
        {
            this.pixels[i] = pixels[i].a;
        }
    }

    public byte GetByte(int x, int y)
    {
        return bytes[texture.width * y + x];
    }

    public void DecodeFromPNG(byte[] data)
    {
        var tex = Texture2DExtensions.Blank(1, 1, TextureFormat.Alpha8);
        tex.LoadImage(data);

        SetPixels32(tex.GetPixels32());

        Apply(true);
    }

    public static void Brush(Texture8 canvas, Rect canvasRect,
                             Texture8 brush,  Rect brushRect,
                             Blend8.Function blend)
    {
        var data = new Blend8.Data();

        int dx = (int) brushRect.xMin - (int) canvasRect.xMin;
        int dy = (int) brushRect.yMin - (int) canvasRect.yMin;

        int cstride = canvas.uTexture.width;
        int bstride =  brush.uTexture.width;

        canvas.dirty = true;

        int xmin = (int) canvasRect.xMin;
        int ymin = (int) canvasRect.yMin;
        int xmax = (int) canvasRect.xMax;
        int ymax = (int) canvasRect.yMax;

        for (int cy = ymin; cy < ymax; ++cy)
        {
            for (int cx = xmin; cx < xmax; ++cx)
            {
                int bx = cx + dx;
                int by = cy + dy;

                int ci = cy * cstride + cx;
                int bi = by * bstride + bx;

                data.canvas = canvas.pixels[ci];
                data.brush  =  brush.pixels[bi];

                canvas.pixels[ci] = blend(data);
            }
        }
    }
}

public class Sprite8 : ManagedSprite<byte>, IDisposable //ISprite<Texture8, byte>, IDisposable
{
    public Texture2D texture
    {
        get
        {
            return mTexture.uTexture;
        }
    }

    public Sprite8(Texture8 texture8,
                   Rect rect,
                   Vector2 pivot)
    {
        this.mTexture = texture8;
        this.rect = rect;
        this.pivot = pivot;

        uSprite = Sprite.Create(texture8.uTexture, rect, pivot, 1, 0, SpriteMeshType.FullRect);
    }

    public Sprite8(Texture8 texture8,
                   Sprite sprite)
    {
        this.mTexture = texture8;
        this.uSprite = sprite;

        rect = sprite.textureRect;
        pivot = sprite.pivot;
    }

    public byte GetByte(int x, int y)
    {
        x += (int) rect.x - (int) pivot.x;
        y += (int) rect.y - (int) pivot.y;

        if (rect.Contains(new Vector2(x, y)))
        {
            return texture8.GetByte(x, y);
        }
        else
        {
            return 0;
        }
    }

    public Brush8 AsBrush(Vector2 position,
                          Blend8.Function blend)
    {
        return new Brush8
        {
            sprite = this,
            position = position,
            blend = blend,
        };
    }

    public bool Brush(Brush8 brush,
                      Vector2 canvasPosition = default(Vector2))
    {
        var canvas = this;

        var b_offset = brush.position - brush.sprite.pivot;
        var c_offset = canvasPosition - canvas.pivot;

        Rect world_rect_brush = brush.sprite.rect;
        world_rect_brush.position = b_offset;

        Rect world_rect_canvas = canvas.rect;
        world_rect_canvas.position = c_offset;
        
        var activeRect = DrawingExtensions.Intersect(world_rect_brush, world_rect_canvas);

        if (activeRect.width < 1 || activeRect.height < 1)
        {
            return false;
        }

        Rect local_rect_brush = activeRect;
        local_rect_brush.x = activeRect.x - world_rect_brush.x + brush.sprite.rect.x;
        local_rect_brush.y = activeRect.y - world_rect_brush.y + brush.sprite.rect.y;

        Rect local_rect_canvas = activeRect;
        local_rect_canvas.x = activeRect.x - world_rect_canvas.x + canvas.rect.x;
        local_rect_canvas.y = activeRect.y - world_rect_canvas.y + canvas.rect.y;

        Texture8.Brush(canvas.mTexture       as Texture8, local_rect_canvas,
                       brush.sprite.mTexture as Texture8, local_rect_brush,
                       brush.blend);

        return true;
    }

    public void Clear(byte value)
    {
        mTexture.Clear(value, rect);
    }

    void IDisposable.Dispose()
    {
        UnityEngine.Object.DestroyImmediate(uSprite);
        Texture8Pooler.FreeTexture(mTexture as Texture8);
    }
}

public struct Brush8
{
    public Sprite8 sprite;
    public Vector2 position;
    public Blend8.Function blend;
    
    public static Sprite8 Sweep(Sprite8 sprite,
                                Vector2 start,
                                Vector2 end)
    {
        var tl = new Vector2(Mathf.Min(start.x, end.x),
                             Mathf.Min(start.y, end.y));

        int width  = (int) Mathf.Abs(end.x - start.x) + (int) sprite.rect.width;
        int height = (int) Mathf.Abs(end.y - start.y) + (int) sprite.rect.height;
        
        var rect = new Rect(0, 0, width, height);

        var texture8 = Texture8Pooler.GetTexture(width, height);
        var sprite8 = new Sprite8(texture8, rect, sprite.pivot - tl);
        sprite8.Clear(0);

        {
            var brush_ = new Brush8 { sprite = sprite, blend = Blend8.mask };

            Bresenham.PlotFunction plot = delegate (int x, int y)
            {
                brush_.position.x = x;
                brush_.position.y = y;

                sprite8.Brush(brush_);
            };

            Bresenham.Line((int)start.x,
                           (int)start.y,
                           (int)end.x,
                           (int)end.y,
                           plot);
        }

        return sprite8;
    }
}

public static class Texture8Pooler
{
    private static List<Texture8> textures = new List<Texture8>();

    public static Texture8 GetTexture(int width, int height)
    {
        Texture8 texture8;

        if (textures.Count > 0)
        {
            int last = textures.Count - 1;

            texture8 = textures[last];
            textures.RemoveAt(last);
        }
        else
        {
            var tex = Texture2DExtensions.Blank(256, 256, TextureFormat.Alpha8);

            texture8 = new Texture8(tex);
        }

        return texture8;
    }

    public static void FreeTexture(Texture8 texture)
    {
        textures.Add(texture);
    }
}
