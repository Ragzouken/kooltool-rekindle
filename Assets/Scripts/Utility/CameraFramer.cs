using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CameraFramer : MonoBehaviour 
{
    public Canvas canvas;
    public Camera camera;
    public int baseWidth, baseHeight;

    [Range(0, 1)]
    public float alignment;

    private void Update()
    {
#if UNITY_EDITOR
        var res = Handles.GetMainGameViewSize();
#else
        var res = new Vector2(Screen.currentResolution.width,
                              Screen.currentResolution.height);
#endif

        int xscale = (int) res.x / baseWidth;
        int yscale = (int) res.y / baseHeight;

        int scale = Mathf.Min(xscale, yscale);
        int width = baseWidth * scale;
        int height = baseHeight * scale;

        int xmargin = ((int) res.x - width) / 2;
        int ymargin = ((int) res.y - height) / 2;

        Rect rect;

        if (res.x > res.y)
        {
            rect = new Rect(Mathf.Lerp(ymargin, res.x - width - ymargin, alignment), ymargin, width, height);
        }
        else
        {
            rect = new Rect(xmargin, Mathf.Lerp(xmargin, res.y - height - xmargin, alignment), width, height);
        }

        camera.pixelRect = rect;
        canvas.scaleFactor = scale;
        canvas.referencePixelsPerUnit = 1;
    }
}
