using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;

internal class FastQueue<T> where T : struct
{
    public T[] items;
    public int mask;
    public uint tail, head;
    
    public FastQueue(int capacity = 16)
    {
        capacity = 1 << (int) Math.Ceiling(Math.Log(capacity, 2)); // calculate closest power of two
        items = new T[capacity];
        mask = capacity - 1;
        tail = head = 0;
    }
    
    public int Count
    {
        get { return (int) (tail - head); }
    }
    
    public void Enqueue(T item)
    {
        if (tail - head == items.Length)
        {
            DoubleCapacity();
        }
        
        items[tail++ & mask] = item;
    }
    
    public void FastEnqueue(T item)
    {
        items[tail++ & mask] = item;
    }
    
    public T Dequeue()
    {
        return items[head++ & mask];
    }
    
    private void DoubleCapacity()
    {
        var capacity = items.Length << 1;
        
        if ((head & mask) == 0)
        {
            Array.Resize(ref items, capacity);
        }
        else
        {
            int size = items.Length;
            var items2 = new T[capacity];
            int length = (int) (size - head & mask);
            Array.Copy(items, head & mask, items2, 0, length);
            Array.Copy(items, 0, items2, length, size - length);
            
            items = items2;
            head = 0;
            tail = (uint) size;
        }
        
        mask = capacity - 1;
    }
}

namespace PixelDraw
{
    public static class TextureExtension
    {
        public static void FloodFillArea(this Texture2D aTex, 
                                         int startX, 
                                         int startY, 
                                         Color32 toValue)
        {
            int w = aTex.width, h = aTex.height;

            if ((w & (w - 1)) != 0
             || (h & (h - 1)) != 0)
            {
                FloodFillAreaNPO2(aTex, startX, startY, toValue, new Rect(0, 0, aTex.width, aTex.height));
            }

            Color32[] colors = aTex.GetPixels32();

            int shift = (int) Math.Round(Math.Log(colors.Length, 4)); // if the array's length is (2^x * 2^x), then shift = x
            int startIndex = startX + (startY << shift);
            
            var fromValue = colors[startIndex];
            
            int size = 1 << shift;
            int sizeMinusOne = size - 1;
            int xMask = size - 1;
            int minIndexForVerticalCheck = size;
            int maxIndexForVerticalCheck = colors.Length - size - 1;
            
            colors[startIndex] = toValue;
            
            var queue = new FastQueue<int>();
            queue.Enqueue(startIndex);
            
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index & xMask;
                
                if (x > 0)
                {
                    var test = colors[index - 1];

                    if (test.r == fromValue.r
                     && test.g == fromValue.g
                     && test.b == fromValue.b)
                    {
                        colors[index - 1] = toValue;
                        queue.Enqueue(index - 1);
                    }
                }
                if (x < sizeMinusOne)
                {
                    var test = colors[index + 1];

                    if (test.r == fromValue.r
                     && test.g == fromValue.g
                     && test.b == fromValue.b)
                    {
                        colors[index + 1] = toValue;
                        queue.Enqueue(index + 1);
                    }
                }
                if (index >= minIndexForVerticalCheck)
                {
                    var test = colors[index - size];

                    if (test.r == fromValue.r
                     && test.g == fromValue.g
                     && test.b == fromValue.b)
                    {
                        colors[index - size] = toValue;
                        queue.Enqueue(index - size);
                    }
                }
                if (index <= maxIndexForVerticalCheck)
                {
                    var test = colors[index + size];

                    if (test.r == fromValue.r
                     && test.g == fromValue.g
                     && test.b == fromValue.b)
                    {
                        colors[index + size] = toValue;
                        queue.Enqueue(index + size);
                    }
                }
            }

            aTex.SetPixels32(colors);
        }

        public struct Point
        {
            public short x;
            public short y;
            public Point(short aX, short aY) { x = aX; y = aY; }
            public Point(int aX, int aY) : this((short)aX, (short)aY) { }
        }

        public static Color[] GetPixelRect(this Texture2D texture, Rect rect)
        {
            return texture.GetPixels((int) rect.x, (int) rect.y, (int) rect.width, (int) rect.height);
        }

        public static void SetPixelRect(this Texture2D texture, Rect rect, Color[] pixels)
        {
            texture.SetPixels((int) rect.x, (int) rect.y, (int) rect.width, (int) rect.height, pixels);
        }

        public static void FloodFillAreaNPO2(this Texture2D aTex, 
                                             int aX, int aY, 
                                             Color32 aFillColor,
                                             Rect bounds)
        {
            Color[] colors = aTex.GetPixelRect(bounds);

            int w = (int) bounds.width;
            int h = (int) bounds.height;

            Color refCol = colors[aX + aY * w];
            var nodes = new FastQueue<Point>();
            nodes.Enqueue(new Point(aX, aY));
            while (nodes.Count > 0)
            {
                Point current = nodes.Dequeue();
                for (int i = current.x; i < w; i++)
                {
                    Color C = colors[i + current.y * w];
                    if (C != refCol || C == aFillColor)
                        break;
                    colors[i + current.y * w] = aFillColor;
                    if (current.y + 1 < h)
                    {
                        C = colors[i + current.y * w + w];
                        if (C == refCol && C != aFillColor)
                            nodes.Enqueue(new Point(i, current.y + 1));
                    }
                    if (current.y - 1 >= 0)
                    {
                        C = colors[i + current.y * w - w];
                        if (C == refCol && C != aFillColor)
                            nodes.Enqueue(new Point(i, current.y - 1));
                    }
                }
                for (int i = current.x - 1; i >= 0; i--)
                {
                    Color C = colors[i + current.y * w];
                    if (C != refCol || C == aFillColor)
                        break;
                    colors[i + current.y * w] = aFillColor;
                    if (current.y + 1 < h)
                    {
                        C = colors[i + current.y * w + w];
                        if (C == refCol && C != aFillColor)
                            nodes.Enqueue(new Point(i, current.y + 1));
                    }
                    if (current.y - 1 >= 0)
                    {
                        C = colors[i + current.y * w - w];
                        if (C == refCol && C != aFillColor)
                            nodes.Enqueue(new Point(i, current.y - 1));
                    }
                }
            }

            aTex.SetPixelRect(bounds, colors);
        }

        public static IEnumerator FloodFillAreaCR(this Texture2D aTex, 
                                                  int aX, int aY, 
                                                  Color aFillColor,
                                                  Rect bounds,
                                                  int chunksize)
        {
            Color[] colors = aTex.GetPixelRect(bounds);
            
            int w = (int) bounds.width;
            int h = (int) bounds.height;
            
            Color refCol = colors[aX + aY * w];
            var nodes = new FastQueue<Point>();
            nodes.Enqueue(new Point(aX, aY));

            int done = 0;

            while (nodes.Count > 0)
            {
                if (done >= chunksize)
                {
                    done = 0;

                    aTex.SetPixelRect(bounds, colors);
                    aTex.Apply();

                    yield return new WaitForEndOfFrame();
                }

                ++done;

                Point current = nodes.Dequeue();
                for (int i = current.x; i < w; i++)
                {
                    Color C = colors[i + current.y * w];
                    if (C != refCol || C == aFillColor)
                        break;
                    colors[i + current.y * w] = aFillColor;
                    if (current.y + 1 < h)
                    {
                        C = colors[i + current.y * w + w];
                        if (C == refCol && C != aFillColor)
                            nodes.Enqueue(new Point(i, current.y + 1));
                    }
                    if (current.y - 1 >= 0)
                    {
                        C = colors[i + current.y * w - w];
                        if (C == refCol && C != aFillColor)
                            nodes.Enqueue(new Point(i, current.y - 1));
                    }
                }

                for (int i = current.x - 1; i >= 0; i--)
                {
                    Color C = colors[i + current.y * w];
                    if (C != refCol || C == aFillColor)
                        break;
                    colors[i + current.y * w] = aFillColor;
                    if (current.y + 1 < h)
                    {
                        C = colors[i + current.y * w + w];
                        if (C == refCol && C != aFillColor)
                            nodes.Enqueue(new Point(i, current.y + 1));
                    }
                    if (current.y - 1 >= 0)
                    {
                        C = colors[i + current.y * w - w];
                        if (C == refCol && C != aFillColor)
                            nodes.Enqueue(new Point(i, current.y - 1));
                    }
                }
            }

            aTex.SetPixelRect(bounds, colors);
            aTex.Apply();
        }
    }
}
