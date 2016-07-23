﻿using UnityEngine;
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

public class Changes
{
    public List<Sprite> sprites = new List<Sprite>();

    public void ApplyTextures()
    {
        foreach (var texture in sprites.Select(sprite => sprite.texture).Distinct())
        {
            texture.Apply();
        }
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

    public void SweepSprite(Changes changes,
                            Sprite sprite,
                            Blend.Function blend,
                            Vector2 begin,
                            Vector2 end)
    {
        PixelDraw.Bresenham.Line((int) begin.x,
                                 (int) begin.y,
                                 (int) end.x,
                                 (int) end.y,
                                 (x, y) => { Brush(changes, new Brush { position = new Vector2(x, y), blend = blend, sprite = sprite }); return true; });
    }

    public void Brush(Changes changes, Brush brush)
    {
        //brush.position -= brush.sprite.pivot;
            
        Vector2 cell_tl, local, cell;
        Sprite sprite;

        Vector2 adjusted = brush.position - brush.sprite.pivot;

        adjusted.GridCoords(cellSize, out cell_tl, out local);
         
        var gw = Mathf.CeilToInt((brush.sprite.rect.width  + local.x) / cellSize);
        var gh = Mathf.CeilToInt((brush.sprite.rect.height + local.y) / cellSize);

        for (int y = 0; y < gh; ++y)
        {
            for (int x = 0; x < gw; ++x)
            {
                cell.x = cell_tl.x + x;
                cell.y = cell_tl.y + y;

                // TODO: track changes

                if (cells.TryGetValue(cell, out sprite))
                {
                    sprite.Brush(brush, cell * cellSize);

                    changes.sprites.Add(sprite);

                    //Changed.Set(cell, true);
                }
                else
                {
                    // new cell?
                }
            }
        }
    }

    public Color GetPixel(Vector2 position)
    {
        Vector2 cell, local;
        Sprite sprite;

        position.GridCoords(cellSize, out cell, out local);

        return cells.TryGetValue(cell, out sprite)
             ? sprite.GetPixel(local)
             : Color.clear;
    }

    public void Apply()
    {
        foreach (var thing in cells.Values) thing.Apply();
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
