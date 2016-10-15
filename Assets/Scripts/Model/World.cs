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
        where T : class, ICopyable<T>, new()
    {
        if (original == null) return null; 

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
    public TextureByte texture8;

    [JsonIgnore]
    public Texture2D uTexture
    {
        get
        {
            return texture8.uTexture;
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
        var tex = Texture2DExtensions.Blank(1, 1);
        tex.LoadImage(System.IO.File.ReadAllBytes(path));

        texture8.SetPixels32(tex.GetPixels32());
        texture8.Apply();
    }

    void IResource.SaveFinalise(Project project)
    {
        if (!dirty) return;

        id = id == "" ? Guid.NewGuid().ToString() : id;

        System.IO.File.WriteAllBytes(path, texture8.uTexture.EncodeToPNG());
    }

    public TextureResource() { }

    public TextureResource(TextureByte texture8)
    {
        this.texture8 = texture8;
    }

    public static implicit operator Texture2D(TextureResource resource)
    {
        return resource.uTexture;
    }

    public void Copy(Copier copier, TextureResource copy)
    {
        copy.texture8 = new TextureByte(texture8.uTexture.width, texture8.uTexture.height);
        Array.Copy(texture8.pixels, copy.texture8.pixels, texture8.pixels.Length);
        copy.texture8.dirty = true;

        copy.id = id;
    }
}

public class SpriteResource : IResource, ICopyable<SpriteResource>
{
    public class Change : IChange
    {
        public SpriteResource sprite;
        public byte[] before, after;

        public Change(SpriteResource sprite)
        {
            this.sprite = sprite;

            before = sprite.sprite8.GetPixels();
            after = new byte[sprite.sprite8.rect.width * sprite.sprite8.rect.height];
        }

        void IChange.Redo(Changes changes)
        {
            sprite.sprite8.SetPixels(after);
        }

        void IChange.Undo(Changes changes)
        {
            sprite.sprite8.GetPixels(after);
            sprite.sprite8.SetPixels(before);
        }
    }

    public TextureResource texture;
    public Vector2 pivot;
    public Rect rect;

    [JsonIgnore]
    public ManagedSprite<byte> sprite8;

    [JsonIgnore]
    public Sprite uSprite
    {
        get
        {
            return sprite8.uSprite;
        }
    }

    bool IResource.LoadFinalisable(Project project)
    {
        return project.LoadFinalised(texture);
    }

    void IResource.LoadFinalise(Project project)
    {
        sprite8 = new ManagedSprite<byte>(texture.texture8, rect, pivot);
    }

    void IResource.SaveFinalise(Project project)
    {
    }

    public SpriteResource() { }

    public SpriteResource(TextureResource texture, Sprite sprite)
    {
        this.sprite8 = new ManagedSprite<byte>(texture.texture8, sprite);
        this.texture = texture;

        pivot = sprite.pivot;
        rect = sprite.textureRect;
    }

    public SpriteResource(TextureResource texture, ManagedSprite<byte> sprite8)
    {
        this.sprite8 = sprite8;
        this.texture = texture;

        pivot = sprite8.pivot;
        rect = sprite8.rect;
    }

    public static implicit operator Sprite(SpriteResource resource)
    {
        return resource.sprite8.uSprite;
    }

    public void Copy(Copier copier, SpriteResource copy)
    {
        copy.texture = copier.Copy(texture);
        copy.pivot = pivot;
        copy.rect = rect;
        copy.sprite8 = new ManagedSprite<byte>(copy.texture.texture8, rect, pivot);
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
        //copy.resources = new HashSet<IResource>(resources.Select(resource => copier.CopyFake(resource)));
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

    public bool TryGetActor(IntVector2 position, 
                            out Actor actor,
                            int range=0)
    {
        for (int i = 0; i < actors.Count; ++i)
        {
            actor = actors[i];

            var rect = actor.GetWorldRect();
            rect.Expand(range);

            if (rect.Contains(position))
            {
                for (int y = position.y - range; y < position.y + range + 1; ++y)
                {
                    for (int x = position.x - range; x < position.x + range + 1; ++x)
                    {
                        if (actor.GetPixel(new IntVector2(x, y)) > 0) return true;
                    }
                }
            }
        }

        actor = default(Actor);
        return false;
    }

    public byte GetPixel(IntVector2 position)
    {
        Actor actor;

        if (TryGetActor(position, out actor))
        {
            return actor.GetPixel(position);
        }
        else
        {
            return background.GetPixel(position);
        }
    }
}

public interface IChange
{
    void Undo(Changes changes);
    void Redo(Changes changes);
}

public class Changes
{
    public HashSet<ManagedSprite<byte>> sprites = new HashSet<ManagedSprite<byte>>();

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
        foreach (var sprite in sprites)
        {
            sprite.mTexture.Apply();
        }
    }

    public void Undo()
    {
        foreach (var change in changes.Values)
        {
            change.Undo(this);
        }

        ApplyTextures();
    }

    public void Redo()
    {
        foreach (var change in changes.Values)
        {
            change.Redo(this);
        }

        ApplyTextures();
    }
}

public class ActorAddedChange : IChange
{
    public World world;
    public Actor actor;

    void IChange.Redo(Changes changes)
    {
        world.actors.Add(actor);
    }

    void IChange.Undo(Changes changes)
    {
        world.actors.Remove(actor);
    }
}

public class ActorRemovedChange : IChange
{
    public World world;
    public Actor actor;

    void IChange.Redo(Changes changes)
    {
        world.actors.Remove(actor);
    }

    void IChange.Undo(Changes changes)
    {
        world.actors.Add(actor);
    }
}

public class ImageGrid : ICopyable<ImageGrid>
{
    public class Change : IChange
    {
        public ImageGrid grid;
        public Dictionary<IntVector2, IChange> sprites 
            = new Dictionary<IntVector2, IChange>();
        public Dictionary<IntVector2, SpriteResource> added 
            = new Dictionary<IntVector2, SpriteResource>();

        public void Added(IntVector2 point, SpriteResource sprite)
        {
            added.Add(point, sprite);
        }

        public void Changed(IntVector2 point)
        {
            if (!sprites.ContainsKey(point))
            {
                sprites[point] = new SpriteResource.Change(grid.cells[point]);
            }
        }

        void IChange.Redo(Changes changes)
        {
            foreach (var add in added)
            {
                grid.cells[add.Key] = add.Value;
            }

            foreach (var sprite in sprites.Values)
            {
                sprite.Redo(changes);
            }
        }

        void IChange.Undo(Changes changes)
        {
            foreach (var sprite in sprites.Values)
            {
                sprite.Undo(changes);
            }

            foreach (var add in added)
            {
                grid.cells.Remove(add.Key);
            }
        }
    }

    public Project project;

    [JsonArray]
    public class GridDict : Dictionary<IntVector2, SpriteResource>
    {
        public GridDict() : base() { }
        public GridDict(Dictionary<IntVector2, SpriteResource> dict) : base(dict) { }
    };

    public int cellSize;
    public GridDict cells = new GridDict();

    public void Copy(Copier copier, ImageGrid copy)
    {
        copy.cellSize = cellSize;
        copy.cells = new GridDict(cells.ToDictionary(pair => pair.Key,
                                                     pair => copier.Copy(pair.Value)));
    }

    public SpriteResource AddCell(IntVector2 cell)
    {
        var texture = new TextureResource(new TextureByte(cellSize, cellSize));
        texture.texture8.Clear(0);
        var sprite = new SpriteResource(texture, texture.uTexture.FullSprite(pixelsPerUnit: 1));

        texture.texture8.Apply();

        project.resources.Add(texture);
        project.resources.Add(sprite);

        cells[cell] = sprite;

        return sprite;
    }

    public void Blend(Changes changes, ManagedSprite<byte> sprite8, IntVector2 brushPosition, Blend<byte> blend)
    {
        IntVector2 cell;
        SpriteResource sprite;

        // find the rectangle of cells that contains the brush
        IntVector2 brushMin = brushPosition - sprite8.pivot;
        IntVector2 brushMax = brushMin + new IntVector2(sprite8.rect.width,
                                                        sprite8.rect.height);

        IntVector2 cellMin = brushMin.CellCoords(cellSize);
        IntVector2 cellMax = brushMax.CellCoords(cellSize);

        var chang = changes.GetChange(this, () => new Change { grid = this });

        // apply the brush to all cells it overlaps
        for (int y = cellMin.y; y <= cellMax.y; ++y)
        {
            for (int x = cellMin.x; x <= cellMax.x; ++x)
            {
                cell.x = x;
                cell.y = y;

                if (!cells.TryGetValue(cell, out sprite))
                {
                    sprite = AddCell(cell);
                    chang.Added(cell, sprite);
                }

                chang.Changed(cell);

                sprite.sprite8.Blend(sprite8, blend, brushPosition: brushPosition, canvasPosition: cell * cellSize);
                sprite.texture.dirty = true;

                changes.sprites.Add(sprite.sprite8);
            }
        }
    }
   
    public byte GetPixel(IntVector2 position, byte @default = 0)
    {
        IntVector2 cell, local;
        SpriteResource sprite;

        position.GridCoords(cellSize, out cell, out local);

        return cells.TryGetValue(cell, out sprite)
             ? sprite.sprite8.GetPixel(local.x, local.y)
             : @default;
    }
}

public class Costume
{
    public World world;
    public SpriteResource up, down, left, right;

    public SpriteResource this[Position.Direction direction]
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

    public string dialogue = "";

    public void Copy(Copier copier, Actor copy)
    {
        copy.world = copier.Copy(world);
        copy.dialogue = dialogue;
        copy.costume = costume;
        copy.script = script;
        copy.position = copier.Copy(position);
        copy.state = copier.Copy(state);
    }

    public void Blend(Changes changes, ManagedSprite<byte> sprite8, IntVector2 brushPosition, Blend<byte> blend)
    {
        //chang.Changed(cell);

        var sprite = costume[position.direction];
        var change = changes.GetChange(sprite, () => new SpriteResource.Change(sprite));

        sprite.sprite8.Blend(sprite8, 
                             blend, 
                             brushPosition: brushPosition, 
                             canvasPosition: position.current);
        sprite.texture.dirty = true;

        changes.sprites.Add(sprite.sprite8);
    }

    public IntRect GetWorldRect()
    {
        var sprite = costume[position.direction];

        IntRect rect = sprite.rect;
        rect.Move(-rect.x, -rect.y);
        rect.Move(position.current - sprite.pivot);

        return rect;
    }

    public bool ContainsPoint(IntVector2 position)
    {
        return GetWorldRect().Contains(position);
    }

    public byte GetPixel(IntVector2 position)
    {
        var sprite = costume[this.position.direction];

        position -= (IntVector2) this.position.current;

        return sprite.sprite8.GetPixel(position.x, position.y);
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

    public Position() { }

    public Position(IntVector2 position)
        : this(position, position)
    {
    }

    public Position(IntVector2 prev, IntVector2 next, float progress=0)
    {
        this.prev = prev;
        this.next = next;
        this.progress = progress;
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
