using UnityEngine;
using UnityEngine.Assertions;

namespace PixelDraw
{
    public struct SpriteDrawing : IDrawing
    {
        public Sprite Sprite;

        public SpriteDrawing(Sprite sprite)
        {
            Sprite = sprite;
        }

        public static implicit operator SpriteDrawing(Sprite sprite)
        {
            return new SpriteDrawing(sprite);
        }

        public void Brush(Point offset, Sprite image, Blend.BlendFunction blend)
        {
            PixelDraw.Brush.Apply(image,  offset,
                                  Sprite, new Point(0, 0),
                                  blend);
        }

        public void Fill(Point pixel, Color color)
        {
            Sprite.texture.FloodFillAreaNPO2(pixel.x, pixel.y, 
                                             color, 
                                             Sprite.textureRect);
        }

        public bool Sample(Point pixel, out Color color)
        {
            pixel = pixel.Offset(Sprite.textureRect.position);

            Assert.IsTrue(Sprite.textureRect.Contains((Vector2) pixel), "Fill out of bounds!");

            color = Sprite.texture.GetPixel(pixel.x, pixel.y);

            return true;
        }

        public void Apply()
        {
            Sprite.texture.Apply();
        }
    }
}
