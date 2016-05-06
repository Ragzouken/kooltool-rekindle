using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

public class World
{
    public Color[] palette = new Color[16];

    public List<Map> maps = new List<Map>();
    public List<Actor> actors = new List<Actor>();
}

public class Map
{
    public World world;
}

public class Costume
{
    public World world;
}

public class Actor
{
    public World world;
    public Costume costume;

    public Map map;
    public Position position;
}

[JsonObject(IsReference = false)]
public class Position
{
    public Vector2 prev;
    public Vector2 next;
    public float progress;
}
