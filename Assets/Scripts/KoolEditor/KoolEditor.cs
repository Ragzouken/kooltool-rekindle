using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class KoolEditor : MonoBehaviour 
{
    [HideInInspector]
    public List<Tile> tilePalette = new List<Tile>();
}
