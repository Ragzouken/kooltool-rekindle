using System.Collections.Generic;
using Mathf = UnityEngine.Mathf;

public partial class ManagedTexture<TTexture, TPixel>
    where TTexture : ManagedTexture<TTexture, TPixel>, new()
{
    public class Pooler
    {
        private List<Sprite> sprites = new List<Sprite>();
        private List<TTexture> textures = new List<TTexture>();

        private Blend<TPixel> mask;
        private TPixel transparent;

        public Pooler(Blend<TPixel> mask, TPixel transparent = default(TPixel))
        {
            this.mask = mask;
            this.transparent = transparent;
        }

        public TTexture CreateTexture(int width, int height)
        {
            var texture = new TTexture();
            texture.Resize(width, height);

            return texture;
        }

        public TTexture GetTexture(int width, int height)
        {
            TTexture dTexture;

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

        public Sprite GetSprite(int width,
                                int height,
                                IntVector2 pivot = default(IntVector2))
        {
            var texture = GetTexture(width, height);

            Sprite sprite;

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
                sprite = new Sprite(texture, new IntRect(0, 0, width, height), pivot);
            }

            return sprite;
        }

        public void FreeSprite(Sprite sprite)
        {
            sprite.Dispose();
            sprites.Add(sprite);
        }

        public void FreeTexture(TTexture texture)
        {
            textures.Add(texture);
        }

        private static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }

        public Sprite Copy(Sprite src)
        {
            var dst = GetSprite(src.rect.width,
                                src.rect.height,
                                src.pivot);

            dst.Blend(src, (c, b) => b);

            return dst;
        }

        public Sprite Rotated1(Sprite src)
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

        public Sprite Rotated(Sprite sprite, int rotations)
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

        public Sprite ShearX(Sprite src,
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

        public Sprite ShearY(Sprite src,
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

        public Sprite Sweep(Sprite sprite,
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

        public Sprite Sweep(Sprite sprite,
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

        public void Line(Sprite canvas,
                         IntVector2 start,
                         IntVector2 end,
                         TPixel color,
                         int thickness,
                         Blend<TPixel> blend)
        {
            var line = Line(start, end, color, thickness);
            canvas.Blend(line, blend);

            FreeTexture(line.mTexture);
            FreeSprite(line);
        }

        public Sprite Line(IntVector2 start,
                                  IntVector2 end,
                                  TPixel color,
                                  int thickness)
        {
            var pivot = new IntVector2((thickness - 1) / 2, (thickness - 1) / 2);
            var circle = GetSprite(thickness, thickness, pivot: pivot);
            circle.Clear(default(TPixel));
            Circle(circle, thickness, color);

            var sweep = Sweep(circle, start, end, mask);

            FreeTexture(circle.mTexture);
            FreeSprite(circle);

            return sweep;
        }

        public Sprite Rectangle(int width, int height,
                                       TPixel color,
                                       IntVector2 pivot = default(IntVector2))
        {
            var rect = GetSprite(width, height, pivot);
            rect.Clear(color);

            return rect;
        }

        public Sprite Circle(int diameter, TPixel color)
        {
            var circle = GetSprite(diameter, diameter);

            circle.Clear(default(TPixel));
            Circle(circle, diameter, color);

            return circle;
        }

        private void Circle(Sprite circle,
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

        public static int Sweep(Sprite sweep,
                                Sprite sprite,
                                IntVector2 start,
                                IntVector2 end,
                                Blend<TPixel> blend,
                                int stippleStride = 1,
                                int stippleOffset = 0)
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

            if (steep)
            { Swap(ref x0, ref y0); Swap(ref x1, ref y1); }

            bool reverse = x0 > x1;

            if (reverse)
            { Swap(ref x0, ref x1); Swap(ref y0, ref y1); }

            int dX = (x1 - x0);
            int dY = Mathf.Abs(y1 - y0);

            int err = (dX / 2);
            int ystep = (y0 < y1 ? 1 : -1);
            int y = y0;

            int stippleLength = x1 - x0;
            int stippleFinal = stippleOffset + stippleLength;

            if (reverse)
                stippleOffset = stippleFinal;

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
}
