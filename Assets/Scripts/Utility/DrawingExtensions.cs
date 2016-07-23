using UnityEngine;
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
}
