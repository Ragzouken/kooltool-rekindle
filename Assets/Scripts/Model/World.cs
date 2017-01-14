using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

using kooltool;

using Profiler = UnityEngine.Profiling.Profiler;

/*
public class TextureByteConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TextureByte);
    }

    public override object ReadJson(JsonReader reader, 
                                    Type objectType, 
                                    object existingValue, 
                                    JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);

        var texture = new TextureByte(obj.GetValue("width").Value<int>(), 
                                      obj.GetValue("height").Value<int>());
        
        return texture;
    }

    public override void WriteJson(JsonWriter writer, 
                                   object value, 
                                   JsonSerializer serializer)
    {
        var texture = (TextureByte) value;

        var obj = new JObject();

        obj.Add("width",  texture.width);
        obj.Add("height", texture.height);

        obj.WriteTo(writer);
    }
}
*/

//[JsonConverter(typeof(TextureByteConverter))]
[JsonObject(MemberSerialization = MemberSerialization.OptIn,
            IsReference = true)]
public class KoolTexture : TextureByte, ICopyable<KoolTexture>
{
    [JsonProperty]
    public new int width { get { return base.width; } }
    [JsonProperty]
    public new int height { get { return base.height; } }

    public KoolTexture() : base() { }

    [JsonConstructor]
    public KoolTexture(int width, int height) : base(width, height)
    {
    }

    public void Copy(Copier copier, KoolTexture copy)
    {
        copy.Reformat(width, height, format);
        Array.Copy(pixels, copy.pixels, pixels.Length);
        copy.dirty = true;
    }
}

public class KoolSpriteConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(KoolSprite);
    }

    public override object ReadJson(JsonReader reader, 
                                    Type objectType, 
                                    object existingValue, 
                                    JsonSerializer serializer)
    {
        Profiler.BeginSample("Read JSON KoolSprite");

        var obj = JObject.Load(reader);

        var sprite = new KoolSprite(obj["texture"].ToObject<KoolTexture>(serializer), 
                                    obj["rect"].ToObject<IntRect>(serializer),
                                    obj["pivot"].ToObject<IntVector2>(serializer));

        Profiler.EndSample();

        return sprite;
    }

    public override void WriteJson(JsonWriter writer, 
                                   object value, 
                                   JsonSerializer serializer)
    {
        var sprite = (KoolSprite) value;
        var obj = new JObject();

        obj.Add("texture", JToken.FromObject(sprite.mTexture, serializer));
        obj.Add("rect",    JToken.FromObject(sprite.rect,     serializer));
        obj.Add("pivot",   JToken.FromObject(sprite.pivot,    serializer));

        obj.WriteTo(writer);
    }
}

[JsonConverter(typeof(KoolSpriteConverter))]
public class KoolSprite : ManagedSprite<byte>, ICopyable<KoolSprite>
{ 
    public KoolSprite() : base() { }

    public KoolSprite(KoolTexture mTexture,
                      IntRect rect,
                      IntVector2 pivot = default(IntVector2))
        : base(mTexture, rect, pivot)
    { }

    public void Copy(Copier copier, KoolSprite copy)
    {
        copy.mTexture = copier.Copy(mTexture as KoolTexture);
        copy.rect = rect;
        copy.pivot = pivot;
    }
}

public class KoolSpriteChange : IChange
{
    public KoolSprite sprite;
    public byte[] before, after;

    public KoolSpriteChange(KoolSprite sprite)
    {
        this.sprite = sprite;

        before = sprite.GetPixels();
        after = new byte[sprite.rect.width * sprite.rect.height];
    }

    void IChange.Redo(Changes changes)
    {
        sprite.SetPixels(after);
    }

    void IChange.Undo(Changes changes)
    {
        sprite.GetPixels(after);
        sprite.SetPixels(before);
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
    public Scene world;
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
    public Scene world;
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
        public Dictionary<IntVector2, KoolSprite> added 
            = new Dictionary<IntVector2, KoolSprite>();

        public void Added(IntVector2 point, KoolSprite sprite)
        {
            added.Add(point, sprite);
        }

        public void Changed(IntVector2 point)
        {
            if (!sprites.ContainsKey(point))
            {
                sprites[point] = new KoolSpriteChange(grid.cells[point]);
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

    public kooltool.Project project;

    [JsonConverter(typeof(DictionarySerializer<IntVector2, KoolSprite, GridDict>))]
    public class GridDict : Dictionary<IntVector2, KoolSprite>
    {
        public GridDict() : base() { }
        public GridDict(Dictionary<IntVector2, KoolSprite> dict) : base(dict) { }
    };

    public int cellSize;
    public GridDict cells = new GridDict();

    public void Copy(Copier copier, ImageGrid copy)
    {
        copy.cellSize = cellSize;
        copy.cells = new GridDict(cells.ToDictionary(pair => pair.Key,
                                                     pair => copier.Copy(pair.Value)));
    }

    public KoolSprite AddCell(IntVector2 cell)
    {
        var texture = project.CreateTexture(cellSize, cellSize);
        var sprite = new KoolSprite(texture, new IntRect(0, 0, cellSize, cellSize), IntVector2.zero);

        texture.Clear(0);
        texture.Apply();

        cells[cell] = sprite;

        return sprite;
    }

    public void Blend(Changes changes, ManagedSprite<byte> sprite8, IntVector2 brushPosition, Blend<byte> blend)
    {
        IntVector2 cell;
        KoolSprite sprite;

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

                sprite.Blend(sprite8, blend, brushPosition: brushPosition, canvasPosition: cell * cellSize);
                
                changes.sprites.Add(sprite);
            }
        }
    }
   
    public byte GetPixel(IntVector2 position, byte @default = 0)
    {
        IntVector2 cell, local;
        KoolSprite sprite;

        position.GridCoords(cellSize, out cell, out local);

        return cells.TryGetValue(cell, out sprite)
             ? sprite.GetPixel(local.x, local.y)
             : @default;
    }
}

public class Costume
{
    public KoolSprite up, down, left, right;

    public KoolSprite this[Position.Direction direction]
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
    public Scene world;
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
        var change = changes.GetChange(sprite, () => new KoolSpriteChange(sprite));

        sprite.Blend(sprite8, 
                     blend, 
                     brushPosition: brushPosition, 
                     canvasPosition: position.current);

        changes.sprites.Add(sprite);
    }

    public IntRect GetWorldRect()
    {
        var sprite = costume[position.direction];

        IntRect rect = sprite.rect;
        rect.Move(-rect.x, -rect.y);
        rect.Move((IntVector2) position.current - sprite.pivot);

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

        return sprite.GetPixel(position.x, position.y);
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

    public override string ToString()
    {
        return string.Format("{0} -> {1} ({2:0.00}%)", prev, next, progress);
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
