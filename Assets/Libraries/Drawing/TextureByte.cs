using UnityEngine;

[Newtonsoft.Json.JsonConverter(typeof(TextureByteConverter))]
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

    public static Texture2D temporary;
    public static Blend<byte> mask = (canvas, brush) => brush == 0 ? canvas : brush;

    static TextureByte()
    {
        temporary = Texture2DExtensions.Blank(1, 1);
    }

    public TextureByte(int width, int height) 
        : base(width, height, TextureFormat.Alpha8)
    {
    }

    public override void Apply()
    {
        base.Apply();

        if (dirty)
        {
            uTexture.LoadRawTextureData(pixels);
            uTexture.Apply();
            dirty = false;
        }
    }

    public void SetPixels32(Color32[] pixels)
    {
        for (int i = 0; i < this.pixels.Length; ++i)
        {
            this.pixels[i] = pixels[i].a;
        }

        dirty = true;
    }

    public void DecodeFromPNG(byte[] data)
    {
        temporary.LoadImage(data);
        SetPixels32(temporary.GetPixels32());

        Apply();
    }
}
