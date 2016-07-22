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

    public float timer;
    public List<Actor> actors = new List<Actor>();
    public ImageGrid background = new ImageGrid();

    public World Copy()
    {
        var copier = new Copier();

        return copier.Copy(this);
    }

    public void Copy(Copier copier, World copy)
    {
        palette.CopyTo(copy.palette, 0);

        copy.timer = timer;

        copy.actors.AddRange(actors.Select(actor => copier.Copy(actor)));
        copy.background = copier.Copy(background);
    }
}

public class ImageGrid : ICopyable<ImageGrid>
{
    public int cellSize;
    public Dictionary<Point, Sprite> cells = new Dictionary<Point, Sprite>();

    public void Copy(Copier copier, ImageGrid copy)
    {
        copy.cellSize = cellSize;
        copy.cells = new Dictionary<Point, Sprite>(cells);
    }
}

public class Costume
{
    public World world;

    public Sprite up, down, left, right;

    public Sprite this[Position.Direction direction]
    {
        get
        {
            if (direction == Position.Direction.Right) return right;
            if (direction == Position.Direction.Down) return down;
            if (direction == Position.Direction.Left) return left;
            if (direction == Position.Direction.Up) return up;

            return right;
        }
    }
}

public class Actor : ICopyable<Actor>
{
    public World world;
    public Costume costume;
    public Script script;

    public Position position;
    public State state;

    public void Copy(Copier copier, Actor copy)
    {
        copy.world = copier.Copy(world);
        copy.costume = costume;
        copy.script = script;
        copy.position = copier.Copy(position);
        copy.state = copier.Copy(state);
    }
}

[JsonObject(IsReference = false)]
public class Position : ICopyable<Position>
{
    public enum Direction
    {
        Right,
        Down,
        Left,
        Up,
    }

    public Vector2 prev;
    public Vector2 next;
    public float progress;
    public Direction direction;

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
        copy.direction = direction;
    }
}

public class Script
{
    public Fragment[] fragments;
}

public class Fragment
{
    public string name;
    public string[][] lines;
}

public class State : ICopyable<State>
{
    public string fragment;
    public int line;

    public void Copy(Copier copier, State copy)
    {
        copy.fragment = fragment;
        copy.line = line;
    }
}
