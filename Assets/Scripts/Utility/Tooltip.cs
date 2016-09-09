﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Tooltip : MonoBehaviour 
{
    [SerializeField] private RectTransform bounds;
    [SerializeField] private RectTransform extent;
    [SerializeField] private Text text;
    [SerializeField] private CanvasGroup group;

    private RectTransform source;
    private TooltipBias bias;
    
    private void Update()
    {
        if (source == null) return;

        UIExtensions.RepositionTooltip(transform as RectTransform,
                                       source,
                                       bounds,
                                       extent,
                                       bias);
    }

    public void Show(RectTransform source, 
                     string text,
                     TooltipBias bias)
    {
        this.source = source;
        this.text.text = text;
        this.bias = bias;
        group.alpha = 1;

        Update();
    }

    public void Hide()
    {
        group.alpha = 0;
        source = null;
    }
}
