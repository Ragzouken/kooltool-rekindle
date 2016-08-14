using UnityEngine;
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

    [Test]
    public void Reference01()
    {
        var reference = Resources.Load<Texture2D>("Drawing-Reference-01");

        Assert.AreEqual(Difference(reference, reference), 0, "Reference image doesn't equal itself!");

        var circle3 = Brush.Circle(3, Color.black);
        var circle4 = Brush.Circle(4, Color.black);
        var circle16 = Brush.Circle(16, Color.black);

        var generated = Brush.Rectangle(64, 64, Color.white);
        generated.Brush(circle3.AsBrush(Vector2.one * 4, Blend.alpha));

        Assert.AreNotEqual(Difference(reference, generated.texture), 0, "Generated image should be different to reference at this point!");

        var line1 = Brush.Line(new Vector2(8, 4), new Vector2(12, 4), Color.black, 3);
        generated.Brush(line1.AsBrush(Vector2.zero, Blend.alpha));

        var line2 = Brush.Line(new Vector2(4, 8), new Vector2(8, 12), Color.black, 3);
        generated.Brush(line2.AsBrush(Vector2.zero, Blend.alpha));

        generated.Brush(circle4.AsBrush(new Vector2(6, 18), Blend.alpha));
        generated.Brush(circle4.AsBrush(new Vector2(6, 26), Blend.alpha));
        generated.Brush(circle4.AsBrush(new Vector2(14, 18), Blend.alpha));
        generated.Brush(circle4.AsBrush(new Vector2(14, 26), Blend.alpha));

        generated.Brush(circle16.AsBrush(new Vector2(24, 12), Blend.alpha));

        var lineR = Brush.Line(new Vector2(36, 4), new Vector2(60, 4), Color.red, 6);
        generated.Brush(lineR.AsBrush(Vector2.zero, Blend.alpha));

        var lineB = Brush.Line(new Vector2(36, 4), new Vector2(60, 4), Color.blue, 4);
        generated.Brush(lineB.AsBrush(Vector2.zero, Blend.alpha));

        var lineG = Brush.Line(new Vector2(36, 4), new Vector2(60, 4), Color.green, 2);
        generated.Brush(lineG.AsBrush(Vector2.zero, Blend.alpha));

        int difference = Difference(reference, generated.texture);

        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/Drawing-Reference-01.png", generated.texture.EncodeToPNG());

        Assert.AreEqual(difference, 0, string.Format("Generated image doesn't match reference! ({0} difference)", difference));
    }

    [Test]
    public void LineCoords()
    {
        var generated1 = Brush.Rectangle(64, 64, Color.white);
        var generated2 = Brush.Rectangle(64, 64, Color.white);

        Vector2 start = new Vector2(4, 4);
        Vector2 end = new Vector2(60, 60);

        var lineAbs = Brush.Line(start, end, Color.magenta, 5);
        generated1.Brush(lineAbs.AsBrush(Vector2.zero, Blend.alpha));

        var lineRel = Brush.Line(Vector2.zero, end - start, Color.magenta, 5);
        generated2.Brush(lineRel.AsBrush(start, Blend.alpha));

        SaveOut(generated1, "Drawing-LineCoords-Abs");
        SaveOut(generated2, "Drawing-LineCoords-Rel");

        int difference = Difference(lineAbs.texture, lineRel.texture);

        Assert.AreEqual(difference, 0, string.Format("Images should match! ({0} difference)", difference));
    }

    [Test]
    public void LineSweep()
    {
        var generated1 = Brush.Rectangle(64, 64, Color.white);
        var generated2 = Brush.Rectangle(64, 64, Color.white);

        Vector2 start = new Vector2(4, 4);
        Vector2 end = new Vector2(60, 60);

        var circle5 = Brush.Circle(5, Color.magenta);

        var line = Brush.Line(start, end, Color.magenta, 5);
        generated1.Brush(line.AsBrush(Vector2.zero, Blend.alpha));

        var sweep = Brush.Sweep(circle5, start, end);
        generated2.Brush(sweep.AsBrush(Vector2.zero, Blend.alpha));

        SaveOut(generated1, "Drawing-LineSweep-Line");
        SaveOut(generated2, "Drawing-LineSweep-Sweep");

        int difference = Difference(generated1.texture, generated2.texture);

        Assert.AreEqual(difference, 0, string.Format("Images should match! ({0} difference)", difference));
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
    public void LineSweep_Managed()
    {
        var generated1 = DrawingTexturePooler.Instance.GetSprite(64, 64);
        generated1.Clear(Color.white);
        var generated2 = DrawingTexturePooler.Instance.GetSprite(64, 64);
        generated2.Clear(Color.white);

        Vector2 start = new Vector2(4, 4);
        Vector2 end = new Vector2(60, 60);

        Blend<Color> alpha = (canvas, brush) => Blend.Lerp(canvas, brush, brush.a);

        var circle5 = DrawingTexturePooler.Instance.GetSprite(5, 5, pivot: Vector2.one * 2);
        circle5.Clear(Color.clear);
        Brush8.Circle<Color>(circle5, 5, Color.magenta);

        var line = DrawingBrush.Line(start, end, Color.magenta, 5);
        generated1.Blend(line, alpha);

        var sweep = Brush8.Sweep<Color>(circle5, start, end, DrawingTexturePooler.Instance.GetSprite, alpha);
        generated2.Blend(sweep, alpha);

        sweep.SetPixelAbsolute((int) sweep.pivot.x, (int) sweep.pivot.y, Color.cyan);
        SaveOut(sweep, "sweetest");

        SaveOutExact(generated1, "Drawing-LineSweep-Line-Managed");
        SaveOutExact(generated2, "Drawing-LineSweep-Sweep-Managed");

        int difference = Difference(generated1, generated2);

        Assert.AreEqual(difference, 0, string.Format("Images should match! ({0} difference)", difference));
    }

    [Test]
    public void Reference01_Managed()
    {
        var reference = Resources.Load<Texture2D>("Drawing-Reference-01");

        Assert.AreEqual(Difference(reference, reference), 0, "Reference image doesn't equal itself!");

        var circle3 = DrawingTexturePooler.Instance.GetSprite(3, 3, Vector2.one * 1);
        circle3.Clear(Color.clear);
        Brush8.Circle(circle3, 3, Color.black);

        var circle4 = DrawingTexturePooler.Instance.GetSprite(4, 4, Vector2.one * 2);
        circle4.Clear(Color.clear);
        Brush8.Circle(circle4, 4, Color.black);

        var circle16 = DrawingTexturePooler.Instance.GetSprite(16, 16, Vector2.one * 8);
        circle16.Clear(Color.clear);
        Brush8.Circle(circle16, 16, Color.black);

        Blend<Color> alpha = (canvas, brush) => Blend.Lerp(canvas, brush, brush.a);

        var generated = DrawingTexturePooler.Instance.GetSprite(64, 64);
        generated.Clear(Color.white);
        generated.Blend(circle3, alpha, brushPosition: Vector2.one * 4);

        Assert.AreNotEqual(Difference(reference, GetExact(generated)), 0, "Generated image should be different to reference at this point!");

        var line1 = DrawingBrush.Line(new Vector2(8, 4), new Vector2(12, 4), Color.black, 3);
        generated.Blend(line1, alpha);

        var line2 = DrawingBrush.Line(new Vector2(4, 8), new Vector2(8, 12), Color.black, 3);
        generated.Blend(line2, alpha);

        generated.Blend(circle4, alpha, brushPosition: new Vector2( 6, 18));
        generated.Blend(circle4, alpha, brushPosition: new Vector2( 6, 26));
        generated.Blend(circle4, alpha, brushPosition: new Vector2(14, 18));
        generated.Blend(circle4, alpha, brushPosition: new Vector2(14, 26));

        generated.Blend(circle16, alpha, brushPosition: new Vector2(24, 12));

        var lineR = DrawingBrush.Line(new Vector2(36, 4), new Vector2(60, 4), Color.red, 6);
        generated.Blend(lineR, alpha);

        var lineB = DrawingBrush.Line(new Vector2(36, 4), new Vector2(60, 4), Color.blue, 4);
        generated.Blend(lineB, alpha);

        var lineG = DrawingBrush.Line(new Vector2(36, 4), new Vector2(60, 4), Color.green, 2);
        generated.Blend(lineG, alpha);

        int difference = Difference(reference, GetExact(generated));

        SaveOutExact(generated, "Drawing-Reference-01-Managed.png");

        Assert.AreEqual(difference, 0, string.Format("Generated image doesn't match reference! ({0} difference)", difference));
    }
}
