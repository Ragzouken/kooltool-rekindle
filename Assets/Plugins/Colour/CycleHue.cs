using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CycleHue : MonoBehaviour
{
    [Range(0, 2)]
    public float period = 0.75f;
    [Range(0, 100)]
    public int Saturation = 100;
    [Range(0, 100)]
    public int Lightness = 75;
    [Range(0, 1)]
    public float alpha = 1;

    protected SpriteRenderer Image;

    protected void Awake()
    {
        Image = GetComponent<SpriteRenderer>();
    }

    protected void Update()
    {
        Image.color = Flash(period, Saturation / 100f, Lightness / 100f, alpha);
    }

    public static Color Flash(float period, 
                              float saturation, 
                              float lightness,
                              float alpha = 1)
    {
        float hue = (Time.timeSinceLevelLoad / period) % 1f;

        var RGB = HUSL.HUSLPToRGB(new HUSL.Triplet { a = hue * 360, b = saturation * 100, c = lightness * 100 });

        return new Color((float) RGB.a, (float) RGB.b, (float) RGB.c, alpha);
    }
}
