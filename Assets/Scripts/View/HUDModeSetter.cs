using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class HUDModeSetter : MonoBehaviour 
{
    [SerializeField] private HUD hud;
    [SerializeField] private HUD.Mode mode;

    public void Invoke()
    {
        hud.mode = mode;
    }
}
