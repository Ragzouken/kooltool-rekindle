using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;


public class DrawingTexture
{
    public Texture2D texture;
    public Color32[] colors;

    public DrawingTexture(Texture2D texture)
    {
        this.texture = texture;

        colors = texture.GetPixels32();
    }

    public void Apply()
    {
        texture.SetPixels32(colors);
        texture.Apply();
    }

    public static void Brush(DrawingTexture canvas, Rect canvasRect,
                             DrawingTexture brush, Rect brushRect,
                             Blend32.Function blend)
    {
        var data = new Blend32.Data();

        int dx = (int) brushRect.xMin - (int) canvasRect.xMin;
        int dy = (int) brushRect.yMin - (int) canvasRect.yMin;

        int cstride = canvas.texture.width;
        int bstride =  brush.texture.width;

        for (int cy = (int) canvasRect.yMin; cy < (int) canvasRect.yMax; ++cy)
        {
            for (int cx = (int) canvasRect.xMin; cx < (int) canvasRect.xMax; ++cx)
            {
                int bx = cx + dx;
                int by = cy + dy;

                int ci = cy * cstride + cx;
                int bi = by * bstride + bx;

                data.canvas = canvas.colors[ci];
                data.brush  =  brush.colors[bi];

                canvas.colors[ci] = blend(data);
            }
        }
    }
}

public class DrawingSprite
{
    public DrawingTexture texture;
    public Rect rect;
    public Vector2 pivot;
    public Sprite sprite;

    public DrawingSprite(DrawingTexture texture,
                         Rect rect,
                         Vector2 pivot)
    {
        this.texture = texture;
        this.rect = rect;
        this.pivot = pivot;

        sprite = Sprite.Create(texture.texture, rect, pivot, 1);
    }

    public DrawingSprite(DrawingTexture texture,
                         Sprite sprite)
    {
        this.texture = texture;
        this.sprite = sprite;

        rect = sprite.textureRect;
        pivot = sprite.pivot;
    }

    public DrawingBrush AsBrush(Vector2 position,
                                Blend32.Function blend)
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

        DrawingTexture.Brush(canvas.texture, local_rect_canvas,
                             brush.sprite.texture, local_rect_brush,
                             brush.blend);

        return true;
    }
}

public static class Blend32
{
    public struct Data
    {
        public Color32 canvas;
        public Color32 brush;
    }

    public delegate Color32 Function(Data data);

    public static Function alpha = data => data.brush.a > 0 ? data.brush : data.canvas;
    //public static Function add = data => data.canvas + data.brush;
    //public static Function subtract = data => data.canvas - data.brush;
    //public static Function multiply = data => data.canvas * data.brush;
    public static Function replace = data => data.brush;

    public static Function stencilKeep = data => Color32.Lerp(Color.clear, data.canvas, data.brush.a);
    public static Function stencilCut = data => Color32.Lerp(data.canvas, Color.clear, data.brush.a);
}

public struct DrawingBrush
{
    public DrawingSprite sprite;
    public Vector2 position;
    public Blend32.Function blend;

    public static DrawingSprite Circle(int diameter, Color color)
    {
        int left = Mathf.FloorToInt(diameter / 2f);
        float piv = left / (float)diameter;

        Texture2D image = Texture2DExtensions.Blank(diameter, diameter, Color.clear);

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
        Texture2D image = Texture2DExtensions.Blank(width, height, color);

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

        Vector2 size = new Vector2(Mathf.Abs(end.x - start.x) + sprite.rect.width,
                                   Mathf.Abs(end.y - start.y) + sprite.rect.height);

        var pivot = tl * -1 + sprite.pivot;
        var anchor = new Vector2(pivot.x / size.x, pivot.y / size.y);
        var rect = new Rect(0, 0, size.x, size.y);

        Texture2D image = Texture2DExtensions.Blank((int)size.x, (int)size.y, Color.clear);
        Sprite brush = Sprite.Create(image, rect, anchor, 1);
        brush.name = "Line (Brush)";

        var dTexture = new DrawingTexture(image);
        var dSprite = new DrawingSprite(dTexture, brush);

        {
            var brush_ = new DrawingBrush { sprite = sprite, blend = Blend32.alpha };

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
        var anchor = new Vector2(pivot.x / size.x, pivot.y / size.y);
        var rect = new Rect(0, 0, size.x, size.y);

        Texture2D image = Texture2DExtensions.Blank((int)size.x, (int)size.y, Color.clear);
        Sprite brush = Sprite.Create(image, rect, anchor, 1);
        brush.name = "Line (Brush)";

        var dTexture = new DrawingTexture(image);
        var dSprite = new DrawingSprite(dTexture, brush);

        DrawingSprite circle = Circle(thickness, color);
        {
            var brush_ = new DrawingBrush { sprite = circle, blend = Blend32.alpha };

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
        //UnityEngine.Object.DestroyImmediate(circle.texture);
        //UnityEngine.Object.DestroyImmediate(circle);

        return dSprite;
    }
}
