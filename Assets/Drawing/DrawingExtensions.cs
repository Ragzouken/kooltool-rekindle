﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public static partial class DrawingExtensions
{
    public static Rect Intersect(this Rect a, Rect b)
    {
        return Rect.MinMaxRect(Mathf.Max(a.min.x, b.min.x),
                               Mathf.Max(a.min.y, b.min.y),
                               Mathf.Min(a.max.x, b.max.x),
                               Mathf.Min(a.max.y, b.max.y));
    }
}

public static partial class SpriteExtensions
{
    public static Color GetPixel(this Sprite sprite,
                                 Vector2 position)
    {
        position += sprite.textureRect.position;

        return sprite.textureRect.Contains(position)
             ? sprite.texture.GetPixel((int) position.x, (int) position.y)
             : Color.clear;
    }

    public static Color[] GetPixels(this Sprite sprite)
    {
        return sprite.texture.GetPixels(sprite.textureRect);
    }

    public static void SetPixels(this Sprite sprite, Color[] colors)
    {
        sprite.texture.SetPixels(sprite.textureRect, colors);
    }

    public static Brush AsBrush(this Sprite sprite,
                                Vector2 position,
                                Blend.Function blend)
    {
        return new Brush
        {
            sprite = sprite,
            position = position,
            blend = blend,
        };
    }

    public static bool Brush(this Sprite canvas,
                             Brush brush,
                             Vector2 canvasPosition=default(Vector2))
    {
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

        var local_rect_brush = new Rect(activeRect.x - world_rect_brush.x + brush.sprite.textureRect.x,
                                        activeRect.y - world_rect_brush.y + brush.sprite.textureRect.y,
                                        activeRect.width,
                                        activeRect.height);

        var local_rect_canvas = new Rect(activeRect.x - world_rect_canvas.x + canvas.textureRect.x,
                                         activeRect.y - world_rect_canvas.y + canvas.textureRect.y,
                                         activeRect.width,
                                         activeRect.height);

        Texture2DExtensions.Brush(canvas.texture,       local_rect_canvas,
                                  brush.sprite.texture, local_rect_brush,
                                  brush.blend);

        return true;
    }

    public static void Apply(this Sprite sprite)
    {
        sprite.texture.Apply();
    }
}

public static partial class Texture2DExtensions
{
    public static Color[] GetPixels(this Texture2D texture, 
                                    Rect rect)
    {
        return texture.GetPixels((int) rect.x, 
                                 (int) rect.y, 
                                 (int) rect.width, 
                                 (int) rect.height);
    }

    public static void SetPixels(this Texture2D texture, 
                                 Rect rect, 
                                 Color[] pixels)
    {
        texture.SetPixels((int) rect.x, 
                          (int) rect.y, 
                          (int) rect.width, 
                          (int) rect.height, 
                          pixels);
    }

    public static void Brush(Texture2D canvas, Rect canvasRect,
                             Texture2D brush,  Rect brushRect,
                             Blend.Function blend)
    {
        Color[] canvasColors = canvas.GetPixels(canvasRect);
        Color[] brushColors  = brush.GetPixels(brushRect);

        Assert.IsTrue(canvasColors.Length == brushColors.Length, string.Format("Mismatched texture rects! {0} vs {1}", canvasRect, brushRect));

        var data = new Blend.Data();

        for (int i = 0; i < canvasColors.Length; ++i)
        {
            data.canvas = canvasColors[i];
            data.brush = brushColors[i];

            canvasColors[i] = blend(data);
        }

        canvas.SetPixels(canvasRect, canvasColors);
    }
}

public static class Blend
{
    public struct Data
    {
        public Color canvas;
        public Color brush;
    }

    public delegate Color Function(Data data);

    public static Function alpha    = data => Color.Lerp(data.canvas, data.brush, data.brush.a);
    public static Function add      = data => data.canvas + data.brush;
    public static Function subtract = data => data.canvas - data.brush;
    public static Function multiply = data => data.canvas * data.brush;
    public static Function replace  = data => data.brush;

    public static Function stencilKeep = data => Color.Lerp(Color.clear, data.canvas, data.brush.a);
    public static Function stencilCut  = data => Color.Lerp(data.canvas, Color.clear, data.brush.a);
}

public struct Brush
{
    public Sprite sprite;
    public Vector2 position;
    public Blend.Function blend;

    public static Sprite Circle(int diameter, Color color)
    {
        int left = Mathf.FloorToInt(diameter / 2f);
        float piv = left / (float) diameter;

        Texture2D image = BlankTexture.New(diameter, diameter, Color.clear);

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
        int radiusError = 1-x;
            
        while (x >= y)
        {
            int yoff = (y > 0 ? 1 : 0) * offset;
            int xoff = (x > 0 ? 1 : 0) * offset;

            for (int i = -x + x0; i <= x + x0 + offset; ++i)
            {
                image.SetPixel(i,  y + y0 + yoff, color);
                image.SetPixel(i, -y + y0, color);
            }

            for (int i = -y + y0; i <= y + y0 + offset; ++i)
            {
                image.SetPixel(i,  x + y0 + xoff, color);
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

        return brush;
    }

    public static Sprite Rectangle(int width, int height,
                                   Color color,
                                   float pivotX = 0, float pivotY = 0)
    {
        Texture2D image = BlankTexture.New(width, height, color);

        Sprite brush = Sprite.Create(image, 
                                     new Rect(0, 0, width, height),
                                     new Vector2(pivotX / width, pivotY / height),
                                     1);
        brush.name = "Rectangle (Brush)";

        return brush;
    }

    public static Sprite Line(Vector2 start,
                              Vector2 end,
                              Color color, 
                              int thickness)
    {
        var tl = new Vector2(Mathf.Min(start.x, end.x),
                             Mathf.Min(start.y, end.y));

        int left = Mathf.FloorToInt(thickness / 2f);

        Vector2 size = new Vector2(Mathf.Abs(end.x - start.x) + thickness,
                                   Mathf.Abs(end.y - start.y) + thickness);

        var pivot  = tl * -1 + Vector2.one * left;
        var anchor = new Vector2(pivot.x / size.x, pivot.y / size.y);
        var rect   = new Rect(0, 0, size.x, size.y);

        Texture2D image = BlankTexture.New((int) size.x, (int) size.y, Color.clear);
        Sprite brush = Sprite.Create(image, rect, anchor, 1);
        brush.name = "Line (Brush)";

        Sprite circle = Circle(thickness, color);
        {
            var brush_ = new Brush { sprite = circle, blend = Blend.alpha };

            PixelDraw.Bresenham.PlotFunction plot = delegate (int x, int y)
            {
                brush_.position.x = x;
                brush_.position.y = y;

                brush.Brush(brush_);
                
                return true;
            };

            PixelDraw.Bresenham.Line((int) start.x, 
                                     (int) start.y, 
                                     (int) end.x, 
                                     (int) end.y, 
                                     plot);
        }
        UnityEngine.Object.DestroyImmediate(circle.texture);
        UnityEngine.Object.DestroyImmediate(circle);

        return brush;
    }
}