using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class DrawingTests
{
    private int Difference(Texture2D a, Texture2D b)
    {
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
        AssetDatabase.Refresh();

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

        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/Drawing-LineCoords-Abs.png", generated1.texture.EncodeToPNG());
        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/Drawing-LineCoords-Rel.png", generated2.texture.EncodeToPNG());
        AssetDatabase.Refresh();

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

        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/Drawing-LineSweep-Line.png", generated1.texture.EncodeToPNG());
        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/Drawing-LineSweep-Sweep.png", generated2.texture.EncodeToPNG());
        AssetDatabase.Refresh();

        int difference = Difference(generated1.texture, generated2.texture);

        Assert.AreEqual(difference, 0, string.Format("Images should match! ({0} difference)", difference));
    }


    [Test]
    public void LineSweep_Managed()
    {
        var generated1 = DrawingBrush.Rectangle(64, 64, Color.white);
        var generated2 = DrawingBrush.Rectangle(64, 64, Color.white);

        Vector2 start = new Vector2(4, 4);
        Vector2 end = new Vector2(60, 60);

        Blend<Color> alpha = (canvas, brush) => Blend.Lerp(canvas, brush, brush.a);

        var circle5 = DrawingBrush.Circle(5, Color.magenta);

        var line = DrawingBrush.Line(start, end, Color.magenta, 5);
        generated1.Blend(line, alpha);

        var sweep = Brush8.Sweep(circle5, start, end, DrawingTexturePooler.GetSprite, alpha);
        generated2.Blend(sweep, alpha);

        generated1.mTexture.Apply();
        generated2.mTexture.Apply();

        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/Drawing-LineSweep-Line-Managed.png", generated1.mTexture.uTexture.EncodeToPNG());
        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/Drawing-LineSweep-Sweep-Managed.png", generated2.mTexture.uTexture.EncodeToPNG());
        AssetDatabase.Refresh();

        int difference = Difference(generated1.mTexture.uTexture, generated2.mTexture.uTexture);

        Assert.AreEqual(difference, 0, string.Format("Images should match! ({0} difference)", difference));
    }

    [Test]
    public void Reference01_Managed()
    {
        var reference = Resources.Load<Texture2D>("Drawing-Reference-01");

        Assert.AreEqual(Difference(reference, reference), 0, "Reference image doesn't equal itself!");

        var circle3 = DrawingBrush.Rectangle(3, 3, Color.clear, 1, 1);
        Brush8.Circle(circle3, 3, Color.black);

        var circle4 = DrawingBrush.Rectangle(4, 4, Color.clear, 2, 2);
        Brush8.Circle(circle4, 4, Color.black);

        var circle16 = DrawingBrush.Rectangle(16, 16, Color.clear, 8, 8);
        Brush8.Circle(circle16, 16, Color.black);

        //var circle3 = DrawingBrush.Circle(3, Color.black);
        //var circle4 = DrawingBrush.Circle(4, Color.black);
        //var circle16 = DrawingBrush.Circle(16, Color.black);

        Blend<Color> alpha = (canvas, brush) => Blend.Lerp(canvas, brush, brush.a);

        var generated = DrawingBrush.Rectangle(64, 64, Color.white);
        generated.Blend(circle3, alpha, brushPosition: Vector2.one * 4);

        generated.mTexture.Apply();
        Assert.AreNotEqual(Difference(reference, generated.mTexture.uTexture), 0, "Generated image should be different to reference at this point!");

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

        generated.mTexture.Apply();
        int difference = Difference(reference, generated.mTexture.uTexture);

        System.IO.File.WriteAllBytes(Application.dataPath + "/Drawing/Editor/Output/Drawing-Reference-01-Managed.png", generated.mTexture.uTexture.EncodeToPNG());
        AssetDatabase.Refresh();

        Assert.AreEqual(difference, 0, string.Format("Generated image doesn't match reference! ({0} difference)", difference));
    }
}
