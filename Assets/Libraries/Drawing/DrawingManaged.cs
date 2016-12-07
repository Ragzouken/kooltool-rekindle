using UnityEngine;
using System;
using System.Collections.Generic;

public delegate TPixel Blend<TPixel>(TPixel canvas, TPixel brush);

public abstract class ManagedTexture<TPixel> : IDisposable
{
    public int width;
    public int height;
    public TextureFormat format;

    public Texture2D uTexture;
    public TPixel[] pixels;
    public bool dirty;

    public ManagedSprite<TPixel> FullSprite(IntVector2 pivot)
    {
        return new ManagedSprite<TPixel>(this, new IntRect(0, 0, width, height), pivot);
    }

    protected ManagedTexture() { }

    protected ManagedTexture(int width, int height, TextureFormat format)
    {
        Reformat(width, height, format);
    }

    public void Reformat(int width, int height, TextureFormat format)
    {
        this.width = width;
        this.height = height;
        this.format = format;

        pixels = new TPixel[width * height];
        dirty = true;

        if (uTexture != null)
        {
            UnityEngine.Object.Destroy(uTexture);
            uTexture = null;
        }
    }

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

    public TPixel[] GetPixels(IntRect rect, TPixel[] copy=null)
    {
        copy = copy ?? new TPixel[rect.width * rect.height];

        int tstride = width;
        int cstride = rect.width;

        for (int cy = 0; cy < rect.height; ++cy)
        {
            int tx = rect.xMin;
            int ty = cy + rect.yMin;

            int cx = 0;

            Array.Copy(pixels, ty * tstride + tx, 
                       copy,   cy * cstride + cx, 
                       cstride);
        }

        return copy;
    }

    public void SetPixels(TPixel[] pixels)
    {
        Array.Copy(pixels, this.pixels, this.pixels.Length);

        dirty = true;
    }

    public void SetPixels(IntRect rect, TPixel[] copy)
    {
        int tstride = width;
        int cstride = rect.width;

        for (int cy = 0; cy < rect.height; ++cy)
        {
            int tx = rect.xMin;
            int ty = rect.yMin + cy;

            int cx = 0;

            Array.Copy(copy,   cy * cstride + cx, 
                       pixels, ty * tstride + tx, 
                       cstride);
        }

        dirty = true;
    }

    public void Clear(TPixel value, IntRect rect)
    {
        int stride = width;

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

    public TPixel GetPixel(int x, int y)
    {
        return pixels[width * y + x];
    }

    public void SetPixel(int x, int y, TPixel value)
    {
        pixels[width * y + x] = value;

        dirty = true;
    }

    public virtual void Apply()
    {
        if (uTexture == null)
        {
            uTexture = Texture2DExtensions.Blank(width, height, format);
        }
    }

    public virtual void Dispose()
    {
        UnityEngine.Object.Destroy(uTexture);
    }
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
                Vector2 piv;
                piv.x = pivot.x / (float) this.rect.width;
                piv.y = pivot.y / (float) this.rect.height;
                _uSprite = Sprite.Create(mTexture.uTexture, rect, piv, 1, 0, SpriteMeshType.FullRect);
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

    protected ManagedSprite() { }

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

    public bool Crop(ManagedSprite<TPixel> bounds,
                     IntVector2 canvasPosition = default(IntVector2),
                     IntVector2 brushPosition = default(IntVector2))
    {
        var canvas = this;

        var b_offset = brushPosition - bounds.pivot;
        var c_offset = canvasPosition - canvas.pivot;

        var world_rect_brush = new IntRect(b_offset.x,
                                           b_offset.y,
                                           bounds.rect.width,
                                           bounds.rect.height);

        var world_rect_canvas = new IntRect(c_offset.x,
                                            c_offset.y,
                                            canvas.rect.width,
                                            canvas.rect.height);

        var activeRect = world_rect_brush.Intersect(world_rect_canvas);

        if (activeRect.width < 1 || activeRect.height < 1)
        {
            return false;
        }

        IntRect local_rect_canvas = activeRect;
        local_rect_canvas.Move(-world_rect_canvas.xMin + canvas.rect.xMin,
                               -world_rect_canvas.yMin + canvas.rect.yMin);

        canvas.Crop(local_rect_canvas);

        return true;
    }

    private void Crop(IntRect bounds)
    {
        int stride = mTexture.width;

        int xmin = rect.xMin;
        int ymin = rect.yMin;
        int xmax = rect.xMax;
        int ymax = rect.yMax;

        for (int y = ymin; y < ymax; ++y)
        {
            for (int x = xmin; x < xmax; ++x)
            {
                if (!bounds.Contains(x, y))
                {
                    int i = y * stride + x;

                    mTexture.pixels[i] = default(TPixel);
                }
            }
        }

        mTexture.dirty = true;
    }

    public void Clear(TPixel value)
    {
        mTexture.Clear(value, rect);
    }

    public TPixel GetPixel(int x, int y, TPixel @default = default(TPixel))
    {
        x += rect.x + pivot.x;
        y += rect.y + pivot.y;

        if (rect.Contains(x, y))
        {
            return mTexture.GetPixel(x, y);
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
    
    public TPixel[] GetPixels(TPixel[] copy=null)
    {
        return mTexture.GetPixels(rect, copy);
    }

    public void SetPixels(TPixel[] pixels)
    {
        mTexture.SetPixels(rect, pixels);
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

    public ManagedSprite<TPixel> Copy(ManagedSprite<TPixel> src)
    {
        var dst = GetSprite(src.rect.width,
                            src.rect.height,
                            src.pivot);

        dst.Blend(src, (c, b) => b);

        return dst;
    }

    public ManagedSprite<TPixel> Rotated1(ManagedSprite<TPixel> src)
    {
        int dw = src.rect.height;
        int dh = src.rect.width;

        var dst = GetSprite(dw, 
                            dh,
                            new IntVector2(dw - 1 - src.pivot.y, src.pivot.x));
        
        int ox = dst.rect.xMin - src.rect.xMin;
        int oy = dst.rect.yMin - src.rect.yMin;

        int sstride = src.mTexture.width;
        int dstride = dst.mTexture.width;

        int xmin = src.rect.xMin;
        int ymin = src.rect.yMin;
        int xmax = src.rect.xMax;
        int ymax = src.rect.yMax;
        
        var srcp = src.mTexture.pixels;
        var dstp = dst.mTexture.pixels;

        for (int sy = ymin; sy < ymax; ++sy)
        {
            for (int sx = xmin; sx < xmax; ++sx)
            {
                int rsx = sx - xmin;
                int rsy = sy - ymin;

                int dx = ox + xmin + (dw - 1 - rsy);
                int dy = oy + sx;

                int si = sy * sstride + sx;
                int di = dy * dstride + dx;

                dstp[di] = srcp[si];
            }
        }

        dst.mTexture.dirty = true;

        return dst;
    }

    public ManagedSprite<TPixel> Rotated(ManagedSprite<TPixel> sprite,
                                         int rotations)
    {
        var intermediate = Copy(sprite);

        for (int i = 0; i < rotations; ++i)
        {
            var next = Rotated1(intermediate);

            FreeTexture(intermediate.mTexture);
            FreeSprite(intermediate);

            intermediate = next;
        }

        return intermediate;
    }

    public ManagedSprite<TPixel> ShearX(ManagedSprite<TPixel> src,
                                        float shear,
                                        TPixel background = default(TPixel))
    {
        bool invert = shear < 0;
        shear = Mathf.Abs(shear);

        int grow = (int) (src.rect.width * shear + 0.5f);

        int dw = (int) (src.rect.height * shear) + src.rect.width;
        int dh = src.rect.height;

        int push = invert ? grow : 0;
        int mult = invert ? -1 : 1;

        var pivot = new IntVector2((int) ((1 + shear) * src.pivot.x + 0.5f), 
                                   src.pivot.y);

        var dst = GetSprite(dw, dh, pivot);
        dst.Clear(background);

        int ox = src.rect.xMin - dst.rect.xMin;
        int oy = src.rect.yMin - dst.rect.yMin;

        int sstride = src.mTexture.width;
        int dstride = dst.mTexture.width;

        int xmin = src.rect.xMin;
        int ymin = src.rect.yMin;
        int xmax = src.rect.xMax;
        int ymax = src.rect.yMax;

        var dstp = dst.mTexture.pixels;
        var srcp = src.mTexture.pixels;

        for (int sy = ymin; sy < ymax; ++sy)
        {
            int skew = (int) (shear * (sy - ymin) + 0.5f);

            for (int sx = xmin; sx < xmax; ++sx)
            {
                int dx = ox + sx + skew * mult + push;
                int dy = oy + sy;

                int di = dy * dstride + dx;
                int si = sy * sstride + sx;

                dstp[di] = srcp[si];
            }
        }

        dst.mTexture.dirty = true;

        return dst;
    }

    public ManagedSprite<TPixel> ShearY(ManagedSprite<TPixel> src,
                                        float shear,
                                        TPixel background = default(TPixel))
    {
        bool invert = shear < 0;
        shear = Mathf.Abs(shear);

        int grow = (int) (src.rect.width * shear + 0.5f);

        int dw = src.rect.width;
        int dh = src.rect.height + grow;

        int push = invert ? grow : 0;
        int mult = invert ? -1 : 1;

        var pivot = new IntVector2(src.pivot.x,
                                   (int) ((1 + shear) * src.pivot.y + 0.5f));

        var dst = GetSprite(dw, dh, pivot);
        dst.Clear(background);

        int ox = src.rect.xMin - dst.rect.xMin;
        int oy = src.rect.yMin - dst.rect.yMin;

        int sstride = src.mTexture.width;
        int dstride = dst.mTexture.width;

        int xmin = src.rect.xMin;
        int ymin = src.rect.yMin;
        int xmax = src.rect.xMax;
        int ymax = src.rect.yMax;

        var dstp = dst.mTexture.pixels;
        var srcp = src.mTexture.pixels;

        for (int sx = xmin; sx < xmax; ++sx)
        {
            int skew = (int) (shear * (sx - xmin) + 0.5f);

            for (int sy = ymin; sy < ymax; ++sy)
            {
                int dx = ox + sx;
                int dy = oy + sy + skew * mult + push;

                int di = dy * dstride + dx;
                int si = sy * sstride + sx;

                dstp[di] = srcp[si];
            }
        }

        dst.mTexture.dirty = true;

        return dst;
    }
    
    public ManagedSprite<TPixel> Sweep(ManagedSprite<TPixel> sprite,
                                       IntVector2 start,
                                       IntVector2 end,
                                       Blend<TPixel> blend,
                                       TPixel background = default(TPixel))
    {
        int width = Mathf.Abs(end.x - start.x) + sprite.rect.width;
        int height = Mathf.Abs(end.y - start.y) + sprite.rect.height;

        var sweep = GetSprite(width, height, IntVector2.zero);
        sweep.Clear(background);

        Sweep(sweep, sprite, start, end, blend);

        return sweep;
    }

    public ManagedSprite<TPixel> Sweep(ManagedSprite<TPixel> sprite,
                                       IntVector2 start,
                                       IntVector2 end,
                                       Blend<TPixel> blend,
                                       int stippleStride,
                                       ref int stippleOffset,
                                       TPixel background = default(TPixel))
    {
        int width = Mathf.Abs(end.x - start.x) + sprite.rect.width;
        int height = Mathf.Abs(end.y - start.y) + sprite.rect.height;

        var sweep = GetSprite(width, height, IntVector2.zero);
        sweep.Clear(background);

        stippleOffset = Sweep(sweep, sprite, start, end, blend, stippleStride, stippleOffset);

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

    public static int Sweep(ManagedSprite<TPixel> sweep,
                            ManagedSprite<TPixel> sprite,
                            IntVector2 start,
                            IntVector2 end,
                            Blend<TPixel> blend,
                            int stippleStride=1,
                            int stippleOffset=0)
    {
        var tl = new IntVector2(Mathf.Min(start.x, end.x),
                                Mathf.Min(start.y, end.y));

        sweep.pivot = sprite.pivot - tl;

        IntVector2 position;

        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);

        if (steep)   { Swap(ref x0, ref y0); Swap(ref x1, ref y1); }

        bool reverse = x0 > x1;

        if (reverse) { Swap(ref x0, ref x1); Swap(ref y0, ref y1); }

        int dX = (x1 - x0);
        int dY = Mathf.Abs(y1 - y0);

        int err = (dX / 2);
        int ystep = (y0 < y1 ? 1 : -1);
        int y = y0;

        int stippleLength = x1 - x0;
        int stippleFinal = stippleOffset + stippleLength;

        if (reverse) stippleOffset = stippleFinal;

        for (int x = x0; x <= x1; ++x)
        {
            bool stipple = stippleOffset % stippleStride == 0;

            if (stipple && steep)
            {
                position.x = y;
                position.y = x;

                sweep.Blend(sprite, blend, brushPosition: position);
            }
            else if (stipple)
            {
                position.x = x;
                position.y = y;

                sweep.Blend(sprite, blend, brushPosition: position);
            }

            stippleOffset += reverse ? -1 : 1;

            err = err - dY;

            if (err < 0)
            {
                y += ystep;
                err += dX;
            }
        }

        return stippleFinal;
    }
}
