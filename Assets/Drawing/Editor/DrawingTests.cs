﻿using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class DrawingTests
{
    private int Difference(Texture2D a, Texture2D b)
    {
        a.Apply();
        b.Apply();

        Color32[] pixelsA = a.GetPixels32();
        Color32[] pixelsB = b.GetPixels32();

        Assert.AreEqual(pixelsA.Length, pixelsB.Length, string.Format("Texture {0} is not the same size as Texture {1}!", a.name, b.name));

        int difference = 0;

        for (int i = 0; i < pixelsA.Length; ++i)
        {
            bool equal = pixelsA[i].r == pixelsB[i].r
                      && pixelsA[i].g == pixelsB[i].g
                      && pixelsA[i].b == pixelsB[i].b
                      && pixelsA[i].a == pixelsB[i].a;

            difference += equal ? 0 : 1;
        }

        return difference;
    }

    private int Difference<TPixel>(ManagedSprite<TPixel> a, ManagedSprite<TPixel> b)
    {
        Color[] pixelsA = GetPixels(a);
        Color[] pixelsB = GetPixels(b);

        Assert.AreEqual(pixelsA.Length, pixelsB.Length, string.Format("Sprite {0} is not the same size as Sprite {1}!", a, b));

        int difference = 0;

        for (int i = 0; i < pixelsA.Length; ++i)
        {
            Color32 ca = pixelsA[i];
            Color32 cb = pixelsB[i];

            bool equal = ca.r == cb.r
                      && ca.g == cb.g
                      && ca.b == cb.b
                      && ca.a == cb.a;

            difference += equal ? 0 : 1;
        }

        return difference;
    }

    private void SaveOut(Sprite sprite, string name)
    {
        sprite.texture.Apply();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/" + name + ".png", sprite.texture.EncodeToPNG());
        AssetDatabase.Refresh();
    }

    private void SaveOut<TPixel>(ManagedSprite<TPixel> sprite, string name)
    {
        sprite.mTexture.Apply();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/" + name + ".png", sprite.mTexture.uTexture.EncodeToPNG());
        AssetDatabase.Refresh();
    }

    private Color[] GetPixels<TPixel>(ManagedSprite<TPixel> sprite)
    {
        sprite.mTexture.Apply();

        return sprite.mTexture.uTexture.GetPixels(sprite.uSprite.textureRect);
    }

    private Texture2D GetExact<TPixel>(ManagedSprite<TPixel> sprite)
    {
        var tex = Texture2DExtensions.Blank((int) sprite.rect.width, (int) sprite.rect.height);
        tex.SetPixels(GetPixels(sprite));
        tex.Apply();

        return tex;
    }

    private void SaveOutExact<TPixel>(ManagedSprite<TPixel> sprite, string name)
    {
        SaveOut(GetExact(sprite).FullSprite(), name);
        AssetDatabase.Refresh();
    }

    [Test]
    public void Reference01_Managed()
    {
        var reference = Resources.Load<Texture2D>("Drawing-Reference-01");

        Assert.AreEqual(Difference(reference, reference), 0, "Reference image doesn't equal itself!");

        var circle3  = TextureColor.Pooler.Instance.Circle(3,  Color.black);
        var circle4  = TextureColor.Pooler.Instance.Circle(4,  Color.black);
        var circle16 = TextureColor.Pooler.Instance.Circle(16, Color.black);

        var generated = TextureColor.Pooler.Instance.GetSprite(64, 64);
        generated.Clear(Color.white);
        generated.Blend(circle3, TextureColor.alpha, brushPosition: Vector2.one * 4);

        Assert.AreNotEqual(Difference(reference, GetExact(generated)), 0, "Generated image should be different to reference at this point!");

        var line1 = TextureColor.Pooler.Instance.Line(new Vector2(8, 4), new Vector2(12, 4), Color.black, 3, TextureColor.mask);
        generated.Blend(line1, TextureColor.alpha);

        var line2 = TextureColor.Pooler.Instance.Line(new Vector2(4, 8), new Vector2(8, 12), Color.black, 3, TextureColor.mask);
        generated.Blend(line2, TextureColor.alpha);

        generated.Blend(circle4, TextureColor.alpha, brushPosition: new Vector2( 5, 17));
        generated.Blend(circle4, TextureColor.alpha, brushPosition: new Vector2( 5, 25));
        generated.Blend(circle4, TextureColor.alpha, brushPosition: new Vector2(13, 17));
        generated.Blend(circle4, TextureColor.alpha, brushPosition: new Vector2(13, 25));

        generated.Blend(circle16, TextureColor.alpha, brushPosition: new Vector2(23, 11));

        var lineR = TextureColor.Pooler.Instance.Line(new Vector2(35, 3), new Vector2(59, 3), Color.red, 6, TextureColor.mask);
        generated.Blend(lineR, TextureColor.alpha);

        var lineB = TextureColor.Pooler.Instance.Line(new Vector2(35, 3), new Vector2(59, 3), Color.blue, 4, TextureColor.mask);
        generated.Blend(lineB, TextureColor.alpha);

        var lineG = TextureColor.Pooler.Instance.Line(new Vector2(35, 3), new Vector2(59, 3), Color.green, 2, TextureColor.mask);
        generated.Blend(lineG, TextureColor.alpha);

        int difference = Difference(reference, GetExact(generated));

        SaveOutExact(generated, "Drawing-Reference-01-Managed");

        Assert.AreEqual(difference, 0, string.Format("Generated image doesn't match reference! ({0} difference)", difference));
    }
}
