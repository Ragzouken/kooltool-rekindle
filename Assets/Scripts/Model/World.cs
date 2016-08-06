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

    public T CopyFake<T>(T original)
    {
        object copy;

        if (!TryGetValue(original, out copy))
        {
            Debug.LogError("Don't know how to create copy of abstract type!");
        }

        return (T) copy;
    }
}

public class TextureResource : IResource, ICopyable<TextureResource>
{
    public string id = "";
    [JsonIgnore]
    public bool dirty = false;
    [JsonIgnore]
    public DrawingTexture dTexture;

    [JsonIgnore]
    public Texture2D uTexture
    {
        get
        {
            return dTexture.texture;
        }
    }

    [JsonIgnore]
    public string path
    {
        get
        {
            return Application.persistentDataPath + "/texture" + id + ".png";
        }
    }

    bool IResource.LoadFinalisable(Project project)
    {
        return true;
    }

    void IResource.LoadFinalise(Project project)
    {
        var tex = Texture2DExtensions.Blank(1, 1, Color.clear);
        tex.LoadImage(System.IO.File.ReadAllBytes(path));

        dTexture = new DrawingTexture(tex);
    }

    void IResource.SaveFinalise(Project project)
    {
        if (!dirty)
            Debug.Log("Ignoring texture not direty");

        if (!dirty) return;

        id = id == "" ? Guid.NewGuid().ToString() : id;

        System.IO.File.WriteAllBytes(path, dTexture.texture.EncodeToPNG());
    }

    public TextureResource() { }

    public TextureResource(Texture2D texture)
    {
        this.dTexture = new DrawingTexture(texture);
    }

    public static implicit operator Texture2D(TextureResource resource)
    {
        return resource.uTexture;
    }

    public void Copy(Copier copier, TextureResource copy)
    {
        var tex = Texture2DExtensions.Blank(dTexture.texture.width, dTexture.texture.height, Color.clear);
        tex.SetPixels32(dTexture.texture.GetPixels32());

        copy.dTexture = new DrawingTexture(tex);

        copy.id = id;
    }
}

public class SpriteResource : IResource, ICopyable<SpriteResource>
{
    public TextureResource texture;
    public Vector2 pivot;
    public Rect rect;

    [JsonIgnore]
    public DrawingSprite dSprite;

    [JsonIgnore]
    public Sprite uSprite
    {
        get
        {
            return dSprite.sprite;
        }
    }

    bool IResource.LoadFinalisable(Project project)
    {
        return project.LoadFinalised(texture);
    }

    void IResource.LoadFinalise(Project project)
    {
        dSprite = new DrawingSprite(texture.dTexture, rect, pivot);
    }

    void IResource.SaveFinalise(Project project)
    {
    }

    public SpriteResource() { }

    public SpriteResource(TextureResource texture, Sprite sprite)
    {
        this.dSprite = new DrawingSprite(texture.dTexture, sprite);
        this.texture = texture;

        pivot = sprite.pivot;
        rect = sprite.textureRect;
    }

    public static implicit operator Sprite(SpriteResource resource)
    {
        return resource.dSprite.sprite;
    }

    public void Copy(Copier copier, SpriteResource copy)
    {
        copy.texture = copier.Copy(texture);
        copy.pivot = pivot;
        copy.rect = rect;
        copy.dSprite = new DrawingSprite(copy.texture.dTexture, rect, pivot);
    }
}

public class Project : ICopyable<Project>
{
    public HashSet<IResource> resources = new HashSet<IResource>();
    public World world;

    private HashSet<IResource> unfinalised = new HashSet<IResource>();

    public IEnumerator SaveFinalise()
    {
        foreach (IResource resource in resources)
        {
            resource.SaveFinalise(this);

            yield return null;
        }
    }

    public IEnumerator LoadFinalise()
    {
        unfinalised.UnionWith(resources);

        var ready = new List<IResource>();

        do
        {
            ready.Clear();
            ready.AddRange(unfinalised.Where(resource => resource.LoadFinalisable(this)));

            foreach (var resource in ready)
            {
                resource.LoadFinalise(this);

                yield return null;
            }

            unfinalised.ExceptWith(ready);
        }
        while (ready.Any());

        Assert.IsFalse(unfinalised.Any(), "Didn't finalise all IResources!");
    }

    public bool LoadFinalised(params IResource[] dependencies)
    {
        return !dependencies.Intersect(unfinalised).Any();
    }

    public void Copy(Copier copier, Project copy)
    {
        copy.world = copier.Copy(world);
        // every resource should have already been copied, if not then it
        // doesn't matter that we can't copy it anyway...
        copy.resources = new HashSet<IResource>(resources.Select(resource => copier.CopyFake(resource)));
    }
}

public interface IResource
{
    bool LoadFinalisable(Project project);
    void LoadFinalise(Project project);
    void SaveFinalise(Project project);
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

public interface IChange
{
    void Undo(Changes changes);
    void Redo(Changes changes);
}

public class Changes
{
    public List<DrawingSprite> sprites = new List<DrawingSprite>();

    public Dictionary<object, IChange> changes = new Dictionary<object, IChange>();

    public TChange GetChange<TChange>(object key, Func<TChange> construct)
        where TChange : class, IChange
    {
        IChange change;

        if (changes.TryGetValue(key, out change))
        {
            return change as TChange;
        }
        else
        {
            change = construct();

            changes[key] = change;

            return change as TChange;
        }
    }

    public void ApplyTextures()
    {
        for (int i = 0; i < sprites.Count; ++i)
        {
            sprites[i].dTexture.Apply();
        }
    }

    public void Undo()
    {
        foreach (var change in changes.Values)
        {
            change.Undo(this);
        }
    }

    public void Redo()
    {
        foreach (var change in changes.Values)
        {
            change.Redo(this);
        }
    }
}

public class ImageGrid : ICopyable<ImageGrid>
{
    public class Change : IChange
    {
        public ImageGrid grid;
        public Dictionary<Point, Color32[]> before = new Dictionary<Point, Color32[]>();
        public Dictionary<Point, Color32[]> after = new Dictionary<Point, Color32[]>();
        public HashSet<Point> added = new HashSet<Point>();

        public void Added(Point point)
        {
            added.Add(point);
        }

        public void Changed(Point point)
        {
            Color32[] original;

            if (!before.TryGetValue(point, out original))
            {
                before[point] = grid.cells[point].dSprite.dTexture.texture.GetPixels32();
            }
        }

        void IChange.Redo(Changes changes)
        {
            foreach (var cell in added)
            {
                var rTexture = new TextureResource(Texture2DExtensions.Blank(grid.cellSize, grid.cellSize, Color.clear));
                var sprite = new SpriteResource(rTexture, rTexture.uTexture.FullSprite(pixelsPerUnit: 1));

                grid.cells.Add(cell, sprite);
            }

            foreach (var pair in after)
            {
                before[pair.Key] = grid.cells[pair.Key].dSprite.dTexture.texture.GetPixels32();
                grid.cells[pair.Key].dSprite.dTexture.texture.SetPixels32(after[pair.Key]);
                grid.cells[pair.Key].dSprite.dTexture.Apply();
            }
        }

        void IChange.Undo(Changes changes)
        {
            foreach (var pair in before)
            {
                after[pair.Key] = grid.cells[pair.Key].dSprite.dTexture.texture.GetPixels32();
                grid.cells[pair.Key].dSprite.dTexture.texture.SetPixels32(before[pair.Key]);
                grid.cells[pair.Key].dSprite.dTexture.Apply();
            }

            foreach (var cell in added)
            {
                grid.cells.Remove(cell);
            }
        }
    }

    public Project project;

    [JsonArray]
    public class GridDict : Dictionary<Point, SpriteResource>
    {
        public GridDict() : base() { }
        public GridDict(Dictionary<Point, SpriteResource> dict) : base(dict) { }
    };

    public int cellSize;
    public GridDict cells = new GridDict();

    public void Copy(Copier copier, ImageGrid copy)
    {
        copy.cellSize = cellSize;
        copy.cells = new GridDict(cells.ToDictionary(pair => pair.Key,
                                                     pair => copier.Copy(pair.Value)));
    }

    public void Brush(Changes changes, DrawingBrush brush)
    {
        Vector2 cellMin, cellMax, cell;
        Vector2 local;
        SpriteResource sprite;

        // find the rectangle of cells that contains the brush
        Vector2 brushMin = brush.position - brush.sprite.pivot;
        Vector2 brushMax = brushMin + new Vector2(brush.sprite.rect.width,
                                                  brush.sprite.rect.height);

        brushMin.GridCoords(cellSize, out cellMin, out local);
        brushMax.GridCoords(cellSize, out cellMax, out local);

        var chang = changes.GetChange(this, () => new Change { grid = this });

        // apply the brush to all cells it overlaps
        for (int y = (int) cellMin.y; y <= cellMax.y; ++y)
        {
            for (int x = (int) cellMin.x; x <= cellMax.x; ++x)
            {
                cell.x = x;
                cell.y = y;

                if (!cells.TryGetValue(cell, out sprite))
                { 
                    chang.Added(cell);

                    var texture = new TextureResource(Texture2DExtensions.Blank(cellSize, cellSize, Color.clear));
                    sprite = new SpriteResource(texture, texture.uTexture.FullSprite(pixelsPerUnit: 1));

                    for (int i = 0; i < texture.dTexture.colors.Length; ++i)
                    {
                        texture.dTexture.colors[i] = new Color(0, Mathf.Min((i % 7) / 7f + 0.01f, 1), 0, 0);
                        texture.dTexture.dirty = true;
                    }

                    project.resources.Add(texture);
                    project.resources.Add(sprite);

                    cells[cell] = sprite;
                }

                chang.Changed(cell);

                sprite.dSprite.Brush(brush, cell * cellSize);

                sprite.texture.dTexture.Apply();
                sprite.texture.dirty = true;

                changes.sprites.Add(sprite.dSprite);
            }
        }
    }

    public Color GetPixel(Vector2 position)
    {
        Vector2 cell, local;
        SpriteResource sprite;

        position.GridCoords(cellSize, out cell, out local);

        return cells.TryGetValue(cell, out sprite)
             ? sprite.dSprite.sprite.GetPixel(local)
             : Color.clear;
    }
}

public class Costume
{
    public World world;

    [JsonIgnore]
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
