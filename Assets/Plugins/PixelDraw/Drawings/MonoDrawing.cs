using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace PixelDraw
{
    public class MonoDrawing : MonoBehaviour, IDrawing
    {
        public IDrawing Drawing;

        public virtual void Brush(Point offset, 
                                  Sprite image, 
                                  Blend.BlendFunction blend)
        {
            var rtrans = transform as RectTransform;

            Drawing.Brush(offset - (Point) rtrans.localPosition, image, blend);
        }
        
        public virtual void Fill(Point pixel, Color color)
        {
            Drawing.Fill(pixel, color);
        }

        public virtual bool Sample(Point pixel, out Color color)
        {
            return Drawing.Sample(pixel, out color);
        }
        
        public virtual void Apply()
        {
            Drawing.Apply();
        }
    }
}
