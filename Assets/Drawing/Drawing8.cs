﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using DebuggerDisplay = System.Diagnostics.DebuggerDisplayAttribute;

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

[DebuggerDisplay("Texture8 {texture.name} ({texture.width} x {texture.height})")]
public class Texture8
{
    public bool dirty;
    public Texture2D texture;
    public byte[] bytes;

    public Texture8(Texture2D texture)
    {
        this.texture = texture;

        Color32[] pixels;
        
        pixels = texture.GetPixels32();
        bytes = new byte[pixels.Length];

        for (int i = 0; i < bytes.Length; ++i)
        {
            bytes[i] = pixels[i].a;
        }
    }

    public void Apply(bool force=false)
    {
        if (dirty || force)
        {
            texture.LoadRawTextureData(bytes);
            texture.Apply();
            dirty = false;
        }
    }

    public byte[] GetBytes()
    {
        byte[] copy = new byte[bytes.Length];

        Array.Copy(bytes, copy, bytes.Length);

        return copy;
    }

    public void SetBytes(byte[] bytes)
    {
        Array.Copy(bytes, this.bytes, this.bytes.Length);

        dirty = true;
    }

    public static void Brush(Texture8 canvas, Rect canvasRect,
                             Texture8 brush,  Rect brushRect,
                             Blend8.Function blend)
    {
        var data = new Blend8.Data();

        int dx = (int) brushRect.xMin - (int) canvasRect.xMin;
        int dy = (int) brushRect.yMin - (int) canvasRect.yMin;

        int cstride = canvas.texture.width;
        int bstride =  brush.texture.width;

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

                data.canvas = canvas.bytes[ci];
                data.brush  =  brush.bytes[bi];

                canvas.bytes[ci] = blend(data);
            }
        }
    }

    public void Clear(Rect rect, byte value)
    {
        int stride = texture.width;

        for (int y = (int) rect.yMin; y < (int) rect.yMax; ++y)
        {
            for (int x = (int) rect.xMin; x < (int) rect.xMax; ++x)
            {
                int i = y * stride + x;
                
                bytes[i] = value;
            }
        }

        dirty = true;
    }
}

[DebuggerDisplay("Sprite8 {sprite.name} ({rect}, {pivot})")]
public class Sprite8 : IDisposable
{
    public Texture8 texture8;
    public Rect rect;
    public Vector2 pivot;
    public Sprite sprite;

    public Texture2D texture
    {
        get
        {
            return texture8.texture;
        }
    }

    public Sprite8(Texture8 texture8,
                   Rect rect,
                   Vector2 pivot)
    {
        this.texture8 = texture8;
        this.rect = rect;
        this.pivot = pivot;

        sprite = Sprite.Create(texture8.texture, rect, pivot, 1, 0, SpriteMeshType.FullRect);
    }

    public Sprite8(Texture8 texture8,
                   Sprite sprite)
    {
        this.texture8 = texture8;
        this.sprite = sprite;

        rect = sprite.textureRect;
        pivot = sprite.pivot;
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

        Texture8.Brush(canvas.texture8,       local_rect_canvas,
                       brush.sprite.texture8, local_rect_brush,
                       brush.blend);

        return true;
    }

    public void Clear(byte value)
    {
        texture8.Clear(rect, value);
    }

    void IDisposable.Dispose()
    {
        UnityEngine.Object.DestroyImmediate(sprite);
        Texture8Pooler.FreeTexture(texture8);
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

        var pivot = tl * -1 + sprite.pivot;
        var rect = new Rect(0, 0, width, height);

        var texture8 = Texture8Pooler.GetTexture(width, height);
        var sprite8 = new Sprite8(texture8, rect, pivot);
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
            var tex = Texture2DExtensions.Blank(256, 256, Color.clear, TextureFormat.Alpha8);

            texture8 = new Texture8(tex);
        }

        return texture8;
    }

    public static void FreeTexture(Texture8 texture)
    {
        textures.Add(texture);
    }
}