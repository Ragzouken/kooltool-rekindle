using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public static class BlankTexture
{
    public static Texture2D New(int width, int height, Color32 color)
    {
        var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;

        var pixels = new byte[width * height * 4];

        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i + 0] = color.a;
            pixels[i + 1] = color.r;
            pixels[i + 2] = color.g;
            pixels[i + 3] = color.b;
        }
        
        texture.LoadRawTextureData(pixels);
        texture.Apply();

        return texture;
    }

    public static Sprite FullSprite(this Texture2D texture,
                                    Vector2 pivot=default(Vector2),
                                    int pixelsPerUnit=100)
    {
        var rect = new Rect(0, 0, texture.width, texture.height);

        Sprite sprite = Sprite.Create(texture,
                                      rect,
                                      pivot, 
                                      pixelsPerUnit, 
                                      0U, 
                                      SpriteMeshType.FullRect);

        return sprite;
    }
}
