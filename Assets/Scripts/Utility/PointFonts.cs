using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class PointFonts : MonoBehaviour 
{
    [SerializeField] private Font[] fonts;

    private void Awake()
    {
        for (int i = 0; i < fonts.Length; ++i)
        {
            fonts[i].material.mainTexture.filterMode = FilterMode.Point;
        }
    }
}
