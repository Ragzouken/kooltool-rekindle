using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DrawingTexture : ManagedTexture<Color>
{
    //public bool dirty;
    //public Texture2D texture;
    //public Color[] pixels;

    public DrawingTexture(Texture2D texture)
    {
        this.uTexture = texture;

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

    public static void Brush(DrawingTexture canvas, Rect canvasRect,
                             DrawingTexture brush,  Rect brushRect,
                             Blend.Function blend)
    {
        var data = new Blend.Data();

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

public class DrawingSprite : ManagedSprite<Color>, IDisposable
{
    public Texture2D texture
    {
        get
        {
            return mTexture.uTexture;
        }
    }

    public DrawingSprite(DrawingTexture dTexture,
                         Rect rect,
                         Vector2 pivot)
    {
        this.mTexture = dTexture;
        this.rect = rect;
        this.pivot = pivot;

        uSprite = Sprite.Create(dTexture.uTexture, rect, pivot, 1, 0, SpriteMeshType.FullRect);
    }

    public DrawingSprite(DrawingTexture texture,
                         Sprite sprite)
    {
        this.mTexture = texture;
        this.uSprite = sprite;

        rect = sprite.textureRect;
        pivot = sprite.pivot;
    }

    public DrawingBrush AsBrush(Vector2 position,
                                Blend.Function blend)
    {
        return new DrawingBrush
        {
            sprite = this,
            position = position,
            blend = blend,
        };
    }

    public bool Brush(DrawingBrush brush,
                      Vector2 canvasPosition = default(Vector2))
    {
        var canvas = this;

        var b_offset = brush.position - brush.sprite.pivot;
        var c_offset = canvasPosition - canvas.pivot;

        var world_rect_brush = new Rect(b_offset.x,
                                        b_offset.y,
                                        brush.sprite.rect.width,
                                        brush.sprite.rect.height);

        var world_rect_canvas = new Rect(c_offset.x,
                                         c_offset.y,
                                         canvas.rect.width,
                                         canvas.rect.height);

        var activeRect = DrawingExtensions.Intersect(world_rect_brush, world_rect_canvas);

        if (activeRect.width < 1 || activeRect.height < 1)
        {
            return false;
        }

        var local_rect_brush = new Rect(activeRect.x - world_rect_brush.x + brush.sprite.rect.x,
                                        activeRect.y - world_rect_brush.y + brush.sprite.rect.y,
                                        activeRect.width,
                                        activeRect.height);

        var local_rect_canvas = new Rect(activeRect.x - world_rect_canvas.x + canvas.rect.x,
                                         activeRect.y - world_rect_canvas.y + canvas.rect.y,
                                         activeRect.width,
                                         activeRect.height);

        DrawingTexture.Brush(canvas.mTexture       as DrawingTexture, local_rect_canvas,
                             brush.sprite.mTexture as DrawingTexture, local_rect_brush,
                             brush.blend);

        return true;
    }

    public void Clear(Color color)
    {
        mTexture.Clear(color, rect);
    }

    void IDisposable.Dispose()
    {
        UnityEngine.Object.DestroyImmediate(uSprite);
        DrawingTexturePooler.FreeTexture(mTexture as DrawingTexture);
    }
}

public struct DrawingBrush
{
    public DrawingSprite sprite;
    public Vector2 position;
    public Blend.Function blend;

    public static DrawingSprite Circle(int diameter, Color color)
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
        var dSprite = new DrawingSprite(dTexture, brush);

        return dSprite;
    }

    public static DrawingSprite Rectangle(int width, int height,
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
        var dSprite = new DrawingSprite(dTexture, brush);

        return dSprite;
    }

    public static DrawingSprite Sweep(DrawingSprite sprite,
                                      Vector2 start,
                                      Vector2 end)
    {
        var tl = new Vector2(Mathf.Min(start.x, end.x),
                             Mathf.Min(start.y, end.y));

        int width  = (int) Mathf.Abs(end.x - start.x) + (int) sprite.rect.width;
        int height = (int) Mathf.Abs(end.y - start.y) + (int) sprite.rect.height;

        var pivot = tl * -1 + sprite.pivot;
        var rect = new Rect(0, 0, width, height);

        var dTexture = DrawingTexturePooler.GetTexture(width, height);
        var dSprite = new DrawingSprite(dTexture, rect, pivot);
        dSprite.Clear(Color.clear);

        {
            var brush_ = new DrawingBrush { sprite = sprite, blend = Blend.alpha };

            Bresenham.PlotFunction plot = delegate (int x, int y)
            {
                brush_.position.x = x;
                brush_.position.y = y;

                dSprite.Brush(brush_);
            };

            Bresenham.Line((int)start.x,
                           (int)start.y,
                           (int)end.x,
                           (int)end.y,
                           plot);
        }

        return dSprite;
    }

    public static DrawingSprite Line(Vector2 start,
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
        var dSprite = new DrawingSprite(dTexture, rect, pivot);
        dSprite.Clear(Color.clear);

        DrawingSprite circle = Circle(thickness, color);
        {
            var brush_ = new DrawingBrush { sprite = circle, blend = Blend.alpha };

            Bresenham.PlotFunction plot = delegate (int x, int y)
            {
                brush_.position.x = x;
                brush_.position.y = y;

                dSprite.Brush(brush_);
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

    public static void FreeTexture(DrawingTexture texture)
    {
        textures.Push(texture);
    }
}
