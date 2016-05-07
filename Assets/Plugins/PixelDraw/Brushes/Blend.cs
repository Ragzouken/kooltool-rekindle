using UnityEngine;

namespace PixelDraw
{
    public class Blend
    {
        public delegate Color BlendFunction(Color canvas, Color brush);
        
        public static BlendFunction Alpha = delegate(Color canvas, Color brush)
        {
            return canvas * (1 - brush.a) + brush * brush.a;
        };
        
        public static BlendFunction Add = delegate(Color canvas, Color brush)
        {
            return canvas + brush;
        };

        public static BlendFunction Subtract = delegate(Color canvas, Color brush)
        {
            return canvas - brush;
        };

        public static BlendFunction Multiply = delegate(Color canvas, Color brush)
        {
            return canvas * brush;
        };

        public static BlendFunction Replace = delegate(Color canvas, Color brush)
        {
            return brush;
        };

        public static Color StencilKeep(Color canvas, Color stencil)
        {
            return canvas * stencil.a;
        }

        public static Color StencilCut(Color canvas, Color stencil)
        {
            return canvas * (1f - stencil.a);
        }
    }
}
