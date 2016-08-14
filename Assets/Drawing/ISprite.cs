using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public delegate TPixel Blend<TPixel>(TPixel canvas, TPixel brush);

public interface ITexture<TTexture, TPixel>
    where TTexture : ITexture<TTexture, TPixel>
{
    void Blend(TTexture brushTexture, Blend<TPixel> blend, Rect canvasRect, Rect brushRect);
    void Clear(TPixel value);
    void Clear(TPixel value, Rect rect);
    void Apply();
}

public interface ISprite<TTexture, TPixel>
    where TTexture : ITexture<TTexture, TPixel>
{
    bool Blend(ISprite<TTexture, TPixel> source, Blend<TPixel> blend, int x, int y);
}

public abstract class ManagedTexture<TPixel> //: ITexture<ManagedTexture<TPixel>, TPixel>
    //where TTexture : ITexture<TTexture, TPixel>
{
    public Texture2D uTexture;
    public TPixel[] pixels;
    public bool dirty;

    public void Blend(ManagedTexture<TPixel> brush,
                      Blend<TPixel> blend,
                      Rect canvasRect,
                      Rect brushRect)
    {
        int dx = (int) brushRect.x - (int) canvasRect.x;
        int dy = (int) brushRect.y - (int) canvasRect.y;

        int cstride =       uTexture.width;
        int bstride = brush.uTexture.width;

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

    public void Clear(TPixel value, Rect rect)
    {
        int stride = uTexture.width;

        int xmin = (int) rect.xMin;
        int ymin = (int) rect.yMin;
        int xmax = (int) rect.xMax;
        int ymax = (int) rect.yMax;

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

    public abstract void Apply();
}

public abstract class ManagedSprite<TPixel>
{
    public ManagedTexture<TPixel> mTexture;
    public Sprite uSprite;
    public Rect rect;
    public Vector2 pivot;

    public bool Blend(ManagedSprite<TPixel> brush,
                      Blend<TPixel> blend,
                      Vector2 canvasPosition = default(Vector2),
                      Vector2 brushPosition = default(Vector2))
    {
        var canvas = this;

        var b_offset = brushPosition  - brush.pivot;
        var c_offset = canvasPosition - canvas.pivot;

        var world_rect_brush = new Rect(b_offset.x,
                                        b_offset.y,
                                        brush.rect.width,
                                        brush.rect.height);

        var world_rect_canvas = new Rect(c_offset.x,
                                         c_offset.y,
                                         canvas.rect.width,
                                         canvas.rect.height);

        var activeRect = DrawingExtensions.Intersect(world_rect_brush, world_rect_canvas);

        if (activeRect.width < 1 || activeRect.height < 1)
        {
            return false;
        }

        var local_rect_brush = new Rect(activeRect.x - world_rect_brush.x + brush.rect.x,
                                        activeRect.y - world_rect_brush.y + brush.rect.y,
                                        activeRect.width,
                                        activeRect.height);

        var local_rect_canvas = new Rect(activeRect.x - world_rect_canvas.x + canvas.rect.x,
                                         activeRect.y - world_rect_canvas.y + canvas.rect.y,
                                         activeRect.width,
                                         activeRect.height);

        canvas.mTexture.Blend(brush.mTexture, blend, local_rect_canvas, local_rect_brush);

        return true;
    }
}
