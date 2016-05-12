using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;
using System;

public interface ICopyable<T>
{
    void Copy(Copier copier, T copy);
}

public class Copier : Dictionary<object, object>
{
    public T Copy<T>(T original)
        where T : ICopyable<T>, new()
    {
        object copy;

        if (!TryGetValue(original, out copy))
        {
            copy = new T();

            this[original] = copy;

            original.Copy(this, (T) copy);
        }

        return (T) copy;
    }
}

public class World : ICopyable<World>
{
    public Color[] palette = new Color[16];

    public List<Map> maps = new List<Map>();
    public List<Actor> actors = new List<Actor>();

    public World Copy()
    {
        var copier = new Copier();

        return copier.Copy(this);
    }

    public void Copy(Copier copier, World copy)
    {
        palette.CopyTo(copy.palette, 0);

        copy.actors.AddRange(actors.Select(actor => copier.Copy(actor)));
    }
}

public class Map
{
    public World world;
}

public class Costume
{
    public World world;
}

public class Actor : ICopyable<Actor>
{
    public World world;
    public Costume costume;

    public Map map;
    public Position position;

    public void Copy(Copier copier, Actor copy)
    {
        copy.world = copier.Copy(world);
        copy.position = copier.Copy(position);
    }
}

[JsonObject(IsReference = false)]
public class Position : ICopyable<Position>
{
    public Vector2 prev;
    public Vector2 next;
    public float progress;

    [JsonIgnore]
    public Vector2 current
    {
        get
        {
            return Vector2.Lerp(prev, next, progress);
        }
    }

    [JsonIgnore]
    public bool moving
    {
        get
        {
            return prev != next;
        }
    }

    public void Copy(Copier copier, Position copy)
    {
        copy.prev = prev;
        copy.next = next;
        copy.progress = progress;
    }
}
