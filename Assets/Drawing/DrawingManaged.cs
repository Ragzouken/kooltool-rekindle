using UnityEngine;
using System;
using System.Collections.Generic;

public delegate TPixel Blend<TPixel>(TPixel canvas, TPixel brush);

public abstract class ManagedTexture<TPixel>
{
    public int width;
    public int height;

    public Texture2D uTexture;
    public TPixel[] pixels;
    public bool dirty;

    public void Blend(ManagedTexture<TPixel> brush,
                      Blend<TPixel> blend,
                      IntRect canvasRect,
                      IntRect brushRect)
    {
        int dx = brushRect.xMin - canvasRect.xMin;
        int dy = brushRect.yMin - canvasRect.yMin;

        int cstride = width;
        int bstride = brush.width;

        int xmin = canvasRect.xMin;
        int ymin = canvasRect.yMin;
        int xmax = canvasRect.xMax;
        int ymax = canvasRect.yMax;

        for (int cy = ymin; cy < ymax; ++cy)
        {
            for (int cx = xmin; cx < xmax; ++cx)
            {
                int bx = cx + dx;
                int by = cy + dy;

                int ci = cy * cstride + cx;
                int bi = by * bstride + bx;

                pixels[ci] = blend(pixels[ci], brush.pixels[bi]);
            }
        }

        dirty = true;
    }

    public TPixel[] GetPixels()
    {
        TPixel[] copy = new TPixel[pixels.Length];

        Array.Copy(pixels, copy, pixels.Length);

        return copy;
    }

    public void SetPixels(TPixel[] pixels)
    {
        Array.Copy(pixels, this.pixels, this.pixels.Length);

        dirty = false;
    }

    public void Clear(TPixel value, IntRect rect)
    {
        int stride = uTexture.width;

        int xmin = rect.xMin;
        int ymin = rect.yMin;
        int xmax = rect.xMax;
        int ymax = rect.yMax;

        for (int y = ymin; y < ymax; ++y)
        {
            for (int x = xmin; x < xmax; ++x)
            {
                int i = y * stride + x;

                pixels[i] = value;
            }
        }

        dirty = true;
    }

    public void Clear(TPixel value)
    {
        for (int i = 0; i < pixels.Length; ++i)
        {
            pixels[i] = value;
        }

        dirty = true;
    }

    public TPixel GetPixel(int x, int y, TPixel @default = default(TPixel))
    {
        return pixels[width * y + x];
    }

    public void SetPixel(int x, int y, TPixel value)
    {
        pixels[width * y + x] = value;

        dirty = true;
    }

    public abstract void Apply();
}

public class ManagedSprite<TPixel> : IDisposable
{
    public ManagedTexture<TPixel> mTexture;

    private Sprite _uSprite;
    public Sprite uSprite
    {
        get
        {
            if (_uSprite == null)
            {
                uSprite = Sprite.Create(mTexture.uTexture, rect, pivot, 1, 0, SpriteMeshType.FullRect);
            }

            return _uSprite;
        }

        set
        {
            _uSprite = value;
        }
    }

    public IntRect rect;
    public IntVector2 pivot;

    public ManagedSprite(ManagedTexture<TPixel> mTexture,
                         IntRect rect,
                         IntVector2 pivot)
    {
        this.mTexture = mTexture;
        this.rect = rect;
        this.pivot = pivot;
    }

    public ManagedSprite(ManagedTexture<TPixel> mTexture,
                         Sprite sprite)
    {
        this.mTexture = mTexture;
        rect = sprite.textureRect;
        pivot = sprite.pivot;

        uSprite = sprite;
    }

    public bool Blend(ManagedSprite<TPixel> brush,
                      Blend<TPixel> blend,
                      IntVector2 canvasPosition = default(IntVector2),
                      IntVector2 brushPosition = default(IntVector2))
    {
        var canvas = this;

        var b_offset = brushPosition - brush.pivot;
        var c_offset = canvasPosition - canvas.pivot;

        var world_rect_brush = new IntRect(b_offset.x,
                                           b_offset.y,
                                           brush.rect.width,
                                           brush.rect.height);

        var world_rect_canvas = new IntRect(c_offset.x,
                                            c_offset.y,
                                            canvas.rect.width,
                                            canvas.rect.height);

        var activeRect = world_rect_brush.Intersect(world_rect_canvas);

        if (activeRect.width < 1 || activeRect.height < 1)
        {
            return false;
        }

        IntRect local_rect_brush = activeRect;
        local_rect_brush.Move(-world_rect_brush.xMin + brush.rect.xMin,
                              -world_rect_brush.yMin + brush.rect.yMin);

        IntRect local_rect_canvas = activeRect;
        local_rect_canvas.Move(-world_rect_canvas.xMin + canvas.rect.xMin,
                               -world_rect_canvas.yMin + canvas.rect.yMin);

        canvas.mTexture.Blend(brush.mTexture, blend, local_rect_canvas, local_rect_brush);

        return true;
    }

    public void Clear(TPixel value)
    {
        mTexture.Clear(value, rect);
    }

    public TPixel GetPixel(int x, int y, TPixel @default = default(TPixel))
    {
        x += rect.x - pivot.x;
        y += rect.y - pivot.y;

        if (rect.Contains(x, y))
        {
            return mTexture.GetPixel(x, y, @default);
        }
        else
        {
            return @default;
        }
    }

    public void SetPixelAbsolute(int x, int y, TPixel value)
    {
        x += rect.x;
        y += rect.y;

        if (rect.Contains(x, y))
        {
            mTexture.SetPixel(x, y, value);
        }
    }

    public void Dispose()
    {
        if (_uSprite != null)
        {
            UnityEngine.Object.Destroy(_uSprite);
        }
    }

    public void SetPixelsPerUnit(float ppu)
    {
        Dispose();

        uSprite = Sprite.Create(mTexture.uTexture, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
    }
}

public class ManagedPooler<TPooler, TPixel> : Singleton<TPooler>
    where TPooler : ManagedPooler<TPooler, TPixel>, new()
{
    private List<ManagedSprite<TPixel>> sprites = new List<ManagedSprite<TPixel>>();
    private List<ManagedTexture<TPixel>> textures = new List<ManagedTexture<TPixel>>();

    public virtual ManagedTexture<TPixel> CreateTexture(int width, int height)
    {
        throw new NotImplementedException();
    }

    public ManagedTexture<TPixel> GetTexture(int width, int height)
    {
        ManagedTexture<TPixel> dTexture;

        if (textures.Count > 0 
         && textures[0].width >= width
         && textures[0].height >= height)
        {
            dTexture = textures[textures.Count - 1];
            textures.RemoveAt(textures.Count - 1);
        }
        else
        {
            dTexture = CreateTexture(Mathf.Max(256, width), Mathf.Max(256, height));
        }

        return dTexture;
    }

    public ManagedSprite<TPixel> GetSprite(int width, 
                                           int height, 
                                           IntVector2 pivot = default(IntVector2))
    {
        var texture = GetTexture(width, height);

        ManagedSprite<TPixel> sprite;

        if (sprites.Count > 0)
        {
            sprite = sprites[sprites.Count - 1];
            sprites.RemoveAt(sprites.Count - 1);

            sprite.mTexture = texture;
            sprite.rect = new IntRect(0, 0, width, height);
            sprite.pivot = pivot;
        }
        else
        {
            sprite = new ManagedSprite<TPixel>(texture, new IntRect(0, 0, width, height), pivot);
        }

        return sprite;
    }

    public void FreeSprite(ManagedSprite<TPixel> sprite)
    {
        sprite.Dispose();
        sprites.Add(sprite);
    }

    public void FreeTexture(ManagedTexture<TPixel> texture)
    {
        textures.Add(texture);
    }

    private static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }

    public ManagedSprite<TPixel> Sweep(ManagedSprite<TPixel> sprite,
                                       IntVector2 start,
                                       IntVector2 end,
                                       Blend<TPixel> blend,
                                       TPixel background = default(TPixel))
    {
        int width = Mathf.Abs(end.x - start.x) + sprite.rect.width;
        int height = Mathf.Abs(end.y - start.y) + sprite.rect.height;

        var rect = new Rect(0, 0, width, height);

        var sweep = GetSprite(width, height, IntVector2.zero);
        sweep.Clear(background);

        Sweep(sweep, sprite, start, end, blend);

        return sweep;
    }

    public ManagedSprite<TPixel> Line(IntVector2 start, 
                                      IntVector2 end,
                                      TPixel color,
                                      int thickness,
                                      Blend<TPixel> blend)
    {
        var pivot = new IntVector2((thickness - 1) / 2, (thickness - 1) / 2);
        var circle = GetSprite(thickness, thickness, pivot: pivot);
        circle.Clear(default(TPixel));
        Circle(circle, thickness, color);

        //Blend<TPixel> blend = (canvas, brush) => brush.Equals(default(TPixel)) ? canvas : brush;
        var sweep = Sweep(circle, start, end, blend);

        FreeTexture(circle.mTexture);
        FreeSprite(circle);

        return sweep;
    }

    public ManagedSprite<TPixel> Rectangle(int width, int height,
                                           TPixel color,
                                           IntVector2 pivot = default(IntVector2))
    {
        var rect = GetSprite(width, height, pivot);
        rect.Clear(color);

        return rect;
    }

    public ManagedSprite<TPixel> Circle(int diameter, TPixel color)
    {
        var circle = GetSprite(diameter, diameter);

        circle.Clear(default(TPixel));
        Circle(circle, diameter, color);

        return circle;
    }

    private void Circle(ManagedSprite<TPixel> circle,
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
                circle.SetPixelAbsolute(i, y + y0 + yoff, value);
                circle.SetPixelAbsolute(i, -y + y0, value);
            }

            for (int i = -y + y0; i <= y + y0 + offset; ++i)
            {
                circle.SetPixelAbsolute(i, x + y0 + xoff, value);
                circle.SetPixelAbsolute(i, -x + y0, value);
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

        circle.pivot = IntVector2.one * radius;
    }

    public static void Sweep(ManagedSprite<TPixel> sweep,
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

            if (steep) { Swap(ref x0, ref y0); Swap(ref x1, ref y1); }
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
