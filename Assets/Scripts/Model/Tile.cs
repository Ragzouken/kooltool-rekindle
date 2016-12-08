using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public partial class Tile
{
    public string name;

    public KoolSprite singular;
    public List<KoolSprite> minitiles;
}

public partial class Tile
{
    public bool autotile
    {
        get
        {
            return minitiles != null;
        }
    }

    public KoolSprite thumbnail
    {
        get
        {
            return singular;
        }
    }
}
