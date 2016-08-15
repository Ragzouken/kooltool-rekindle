using UnityEngine;
using System;

public class TextureByte : ManagedTexture<byte>
{
    public class Pooler : ManagedPooler<Pooler, byte>
    {
        public override ManagedTexture<byte> CreateTexture(int width, int height)
        {
            return new TextureByte(width, height);
        }
    }

    public static byte Lerp(byte a, byte b, byte u)
    {
        return (byte)(a + ((u * (b - a)) >> 8));
    }

    public static Blend<byte> mask = (canvas, brush) => brush == 0 ? canvas : brush;

    public TextureByte(int width, int height)
    {
        this.width = width;
        this.height = height;

        uTexture = Texture2DExtensions.Blank(width, height, TextureFormat.Alpha8);
        uTexture.name = "Texture8";
        pixels = new byte[width * height];
        dirty = true;
    }

    // TODO: this isn't safe, what if the texture is the wrong format
    public TextureByte(Texture2D texture)
    {
        width = texture.width;
        height = texture.height;

        uTexture = texture;
        uTexture.name = "Texture8";

        pixels = new byte[width * height];

        SetPixels32(texture.GetPixels32());
    }

    public override void Apply()
    {
        if (dirty)
        {
            uTexture.LoadRawTextureData(pixels);
            uTexture.Apply();
            dirty = false;
        }
    }

    private void SetPixels32(Color32[] pixels)
    {
        for (int i = 0; i < this.pixels.Length; ++i)
        {
            this.pixels[i] = pixels[i].a;
        }

        dirty = true;
    }

    public void DecodeFromPNG(byte[] data)
    {
        var tex = Texture2DExtensions.Blank(1, 1, TextureFormat.Alpha8);
        tex.LoadImage(data);

        SetPixels32(tex.GetPixels32());

        Apply();
    }
}
