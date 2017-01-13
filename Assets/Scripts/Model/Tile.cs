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

    public bool autotile;
    public KoolSprite singular;
    public List<KoolSprite> minitiles;

    public bool _test_wall;
}

public partial class Tile
{
    public KoolSprite thumbnail
    {
        get
        {
            return singular;
        }
    }
}
