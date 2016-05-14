using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace PixelDraw
{
    public class Brush : System.IDisposable
    {
        public readonly Sprite sprite;

        public Brush(Sprite sprite)
        {
            this.sprite = sprite;
        }

        void System.IDisposable.Dispose()
        {
            Dispose(sprite);
        }

        public static implicit operator Sprite(Brush brush)
        {
            return brush.sprite;
        }

        public static implicit operator Brush(Sprite sprite)
        {
            return new Brush(sprite);
        }

        public static Brush Line(Point start,
                                 Point end,
                                 Color color, 
                                 int thickness)
        {
            var tl = new Point(Mathf.Min(start.x, end.x),
                               Mathf.Min(start.y, end.y));

            int left = Mathf.FloorToInt(thickness / 2f);

            Point size = (end - start).Size + new Point(thickness, thickness);
            var pivot  = tl * -1 + Vector2.one * left;
            var anchor = new Vector2(pivot.x / (float) size.x, pivot.y / (float) size.y);
            var rect   = new Rect(0, 0, size.x, size.y);

            Texture2D image = BlankTexture.New(size.x, size.y, Color.clear);
            Brush brush = new Brush(Sprite.Create(image, rect, anchor, 1));
            brush.sprite.name = "Line (Brush)";

            using (Brush circle = Circle(thickness, color))
            {
                Bresenham.PlotFunction plot = delegate (int x, int y)
                {
                    Apply(circle, new Point(x, y),
                          brush, Point.Zero,
                          Blend.Alpha);

                    return true;
                };

                Bresenham.Line(start.x, start.y, end.x, end.y, plot);
            }

            brush.sprite.texture.Apply();

            return brush;
        }

        public static Brush Rectangle(int width, int height,
                                      Color color,
                                      float px = 0, float py = 0)
        {
            Texture2D image = BlankTexture.New(width, height, color);

            Sprite brush = Sprite.Create(image, 
                                         new Rect(0, 0, width, height),
                                         new Vector2(px / width, py / height),
                                         100);
            brush.name = "Rectangle (Brush)";

            return brush;
        }

        public static Brush Circle(int diameter, Color color)
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

        public static void Dispose(Sprite brush)
        {
            Object.Destroy(brush.texture);
            Object.Destroy(brush);
        }

        private class Edge
        {
            public Point yMin;
            public Point yMax;
            public float scanX;

            protected float inc;

            public Edge(Point a, Point b)
            {
                yMin = a.y < b.y ? a : b;
                yMax = a.y < b.y ? b : a;

                if (a.x == b.x) 
                {
                    inc = 0;
                }
                else
                {
                    inc = ((float) a.x - b.x) / ((float) a.y - b.y);
                }

                scanX = yMin.x;
            }

            public void Advance()
            {
                scanX += inc;
            }

            public static int Compare(Edge a, Edge b)
            {
                if (a.yMin.y == b.yMin.y)
                {
                    return a.yMin.x - b.yMin.x;
                }

                return a.yMin.y - b.yMin.y;
            }

            public static int CompareScan(Edge a, Edge b)
            {
                return (int) (a.scanX - b.scanX);
            }

            public override string ToString()
            {
                return string.Format("{0}, {1} -> {2}, {3}", yMin.x, yMin.y, yMax.x, yMax.y);
            }
        }

        public static Sprite Polygon(IList<Point> points, Color color)
        {
            int left   = points[0].x;
            int right  = points[0].x;
            int top    = points[0].y;
            int bottom = points[0].y;

            var edges = new List<Edge>();
            var active = new List<Edge>();

            // build edge table
            for (int i = 0; i < points.Count; ++i)
            {
                int prev = i == 0 ? points.Count - 1 : i - 1;

                left  = Mathf.Min(left,  points[i].x);
                right = Mathf.Max(right, points[i].x);

                bottom = Mathf.Min(bottom, points[i].y);
                top    = Mathf.Max(top,    points[i].y);

                if (points[prev].y != points[i].y)
                {
                    edges.Add(new Edge(points[prev], points[i]));
                }
            }

            var image = BlankTexture.New(right - left, top - bottom, Color.clear);
            var brush = Sprite.Create(image,
                                      new Rect(0, 0, image.width, image.height),
                                      new Vector2(-left   / (float) image.width / 2f, 
                                                  -bottom / (float) image.height/ 2f));
            brush.name = "Polygon (Brush)";

            edges.Sort(Edge.Compare);

            for (int y = bottom; y < top; ++y)
            {
                // remove inactive edges
                foreach (Edge edge in new List<Edge>(active))
                {
                    if (edge.yMin.y >  y
                     || edge.yMax.y <= y)
                    {
                        active.Remove(edge);
                    }
                }

                foreach (Edge edge in edges)
                {
                    if (edge.yMin.y == y)
                    {
                        active.Add(edge);
                    }
                }

                // scanline
                for (int i = 0; i < active.Count; i += 2)
                {
                    int l = (int) active[i  ].scanX;
                    int r = (int) active[i+1].scanX;

                    for (int x = l; x < r; ++x)
                    {
                        image.SetPixel(x - left, y - bottom, color);
                    }
                }

                foreach (Edge edge in active)
                {
                    edge.Advance();
                }

                active.Sort(Edge.CompareScan);
            }

            return brush;
        }

        public static Rect Intersect(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Max(a.min.x, b.min.x),
                                   Mathf.Max(a.min.y, b.min.y),
                                   Mathf.Min(a.max.x, b.max.x),
                                   Mathf.Min(a.max.y, b.max.y));
        }

        public static void Apply(Texture2D canvas, Rect canvasRect,
                                 Texture2D brush,  Rect brushRect,
                                 Blend.BlendFunction blend)
        {
            Color[] canvasColors = canvas.GetPixelRect(canvasRect);
            Color[] brushColors  = brush.GetPixelRect(brushRect);

            Assert.IsTrue(canvasColors.Length == brushColors.Length, string.Format("Mismatched texture rects! {0} vs {1}", canvasRect, brushRect));

            for (int i = 0; i < canvasColors.Length; ++i)
            {
                canvasColors[i] = blend(canvasColors[i], brushColors[i]);
            }

            canvas.SetPixelRect(canvasRect, canvasColors);
        }

        public static bool Apply(Sprite brush,  Point brushPosition,
                                 Sprite canvas, Point canvasPosition,
                                 Blend.BlendFunction blend)
        {
            var b_offset = brushPosition - brush.pivot;
            var c_offset = canvasPosition - canvas.pivot;

            var world_rect_brush = new Rect(b_offset.x,
                                            b_offset.y,
                                            brush.rect.width,
                                            brush.rect.height);

            var world_rect_canvas = new Rect(c_offset.x,
                                             c_offset.y,
                                             canvas.rect.width,
                                             canvas.rect.height);

            var activeRect = Intersect(world_rect_brush, world_rect_canvas);

            if (activeRect.width < 1 || activeRect.height < 1)
            {
                return false;
            }

            var local_rect_brush = new Rect(activeRect.x - world_rect_brush.x + brush.textureRect.x,
                                            activeRect.y - world_rect_brush.y + brush.textureRect.y,
                                            activeRect.width,
                                            activeRect.height);

            var local_rect_canvas = new Rect(activeRect.x - world_rect_canvas.x + canvas.textureRect.x,
                                             activeRect.y - world_rect_canvas.y + canvas.textureRect.y,
                                             activeRect.width,
                                             activeRect.height);

            Apply(canvas.texture, local_rect_canvas,
                  brush.texture,  local_rect_brush,
                  blend);

            return true;
        }

        public static void StencilKeep(Sprite canvas,  Point canvasPosition,
                                       Sprite stencil, Point stencilPosition,
                                       out Sprite brush, out Point brushPostion)
        {
            Apply(canvas,  canvasPosition,
                  stencil, stencilPosition,
                  Blend.StencilKeep);

            brush = stencil;
            brushPostion = stencilPosition;
        }

        public static void StencilCut(Sprite canvas,  Point canvasPosition,
                                      Sprite stencil, Point stencilPosition,
                                      out Sprite brush, out Point brushPostion)
        {
            Apply(stencil, stencilPosition,
                  canvas,  canvasPosition,
                  Blend.StencilCut);

            brush = canvas;
            brushPostion = canvasPosition;
        }

        public static void Texture(Sprite canvas,  Point canvasPosition,
                                   Sprite texture, Point textureOffset)
        {
            for (int y = 0; y < (int) canvas.rect.height; y += (int) texture.rect.height)
            {
                for (int x = 0; x < (int) canvas.rect.width; x += (int) texture.rect.width)
                {
                    Apply(texture, new Point(x, y),
                          canvas,  new Point(canvas.pivot),
                          Blend.Multiply);
                }
            }
        }
    }
}
