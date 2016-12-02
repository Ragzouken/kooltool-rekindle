using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public enum TooltipBias
{
    Up,
    Down,
    Left,
    Right
}

public static partial class UIExtensions 
{
    public static Rect GetWorldRect(this RectTransform rtrans)
    {
        var corners = new Vector3[4];

        rtrans.GetWorldCorners(corners);

        return Rect.MinMaxRect(corners[0].x, corners[0].y,
                               corners[2].x, corners[2].y);
    }

    public static void BoundRectTransform(RectTransform bounded,
                                          RectTransform bounds,
                                          RectTransform extent=null)
    {
        extent = extent ?? bounded; // allow you to bound less than the whole assembly

        // work in world space because it's easier
        var extentRect = extent.GetWorldRect();
        var boundsRect = bounds.GetWorldRect();

        // push the tooltip rect into the division
        float pushR = Mathf.Max(0f, boundsRect.xMin - extentRect.xMin);
        float pushU = Mathf.Max(0f, boundsRect.yMin - extentRect.yMin);

        float pushL = Mathf.Min(0f, boundsRect.xMax - extentRect.xMax);
        float pushD = Mathf.Min(0f, boundsRect.yMax - extentRect.yMax);

        var push = new Vector2(pushL + pushR, pushU + pushD);

        bounded.position += (Vector3) push;
    }

    /// <summary>
    /// Reposition the tooltip so that it is within the bounds but not within
    /// the source
    /// </summary>
    public static void RepositionTooltip(RectTransform tooltip,
                                         RectTransform source,
                                         RectTransform bounds,
                                         RectTransform extent=null,
                                         TooltipBias bias=TooltipBias.Down)
    {
        tooltip.position = source.position;

        extent = extent ?? tooltip; // allow you to bound less than the whole assembly
        
        // work in world space because it's easier
        var extentRect = extent.GetWorldRect();
        var sourceRect = source.GetWorldRect();
        var boundsRect = bounds.GetWorldRect();

        // divide the bounds into four overlapping rectangles on each side of
        // the source rect
        var xMinRect = Rect.MinMaxRect(boundsRect.xMin, boundsRect.yMin,
                                       sourceRect.xMin, boundsRect.yMax);
        var yMinRect = Rect.MinMaxRect(boundsRect.xMin, boundsRect.yMin,
                                       boundsRect.xMax, sourceRect.yMin);
        var xMaxRect = Rect.MinMaxRect(sourceRect.xMax, boundsRect.yMin,
                                       boundsRect.xMax, boundsRect.yMax);
        var yMaxRect = Rect.MinMaxRect(boundsRect.xMin, sourceRect.yMax,
                                       boundsRect.xMax, boundsRect.yMax);

        Rect[] rects;

        if (bias == TooltipBias.Down)
        {
            rects = new[] { yMinRect, yMaxRect, xMinRect, xMaxRect };
        }
        else if (bias == TooltipBias.Up)
        {
            rects = new[] { yMaxRect, yMinRect, xMinRect, xMaxRect };
        }
        else if (bias == TooltipBias.Left)
        {
            rects = new[] { xMinRect, xMaxRect, yMinRect, yMaxRect };
        }
        else
        {
            rects = new[] { xMaxRect, xMinRect, yMinRect, yMaxRect };
        }

        //  bounds                     bounds
        //+------------------------+ +--------+--------+------+
        //|                        | |        |        |      |
        //|                        | |        |        |      |
        //|          yMax          | |  xMin  |        | xMax |
        //|                        | |        |        |      |
        //|                        | |        |        |      |
        //+--------+--------+------+ |        +--------+      |
        //|        |        |      | |        |        |      |
        //|        | source |      | |        | source |      |
        //|        |        |      | |        |        |      |
        //+--------+--------+------+ |        +--------+      |
        //|          yMin          | |        |        |      |
        //|                        | |        |        |      |
        //+------------------------+ +--------+--------+------+

        // find the first division in which the tooltip rect will fit
        for (int i = 0; i < rects.Length; ++i)
        {
            Rect rect = rects[i];

            if (extentRect.width  <= rect.width
             && extentRect.height <= rect.height)
            {
                // push the tooltip rect into the division
                float pushR = Mathf.Max(0f, rect.xMin - extentRect.xMin);
                float pushU = Mathf.Max(0f, rect.yMin - extentRect.yMin);

                float pushL = Mathf.Min(0f, rect.xMax - extentRect.xMax);
                float pushD = Mathf.Min(0f, rect.yMax - extentRect.yMax);

                var push = new Vector2(pushL + pushR, pushU + pushD);

                tooltip.position += (Vector3) push;

                break;
            }
        }
    }
}
