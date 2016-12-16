using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using Newtonsoft.Json;

namespace kooltool
{
    public class TileInstance : ICopyable<TileInstance>
    {
        public Tile tile;
        public List<int> minitiles;

        public void Copy(Copier copier, TileInstance copy)
        {
            copy.tile = tile;
            copy.minitiles = minitiles != null ? new List<int>(minitiles) : null;
        }
    }

    public partial class TileMap : ICopyable<TileMap>
    {
        [JsonConverter(typeof(DictionarySerializer<IntVector2, TileInstance, Data>))]
        public class Data : Dictionary<IntVector2, TileInstance> { }

        public Data tiles = new Data();

        public void Copy(Copier copier, TileMap copy)
        {
            foreach (var pair in tiles)
            {
                copy.tiles.Add(pair.Key, copier.Copy(pair.Value));
            }
        }
    }

    public partial class TileMap
    {
        public TileInstance GetTileAtPosition(IntVector2 position)
        {
            TileInstance instance;

            if (tiles.TryGetValue(position.CellCoords(32), out instance))
            {
                return instance;
            }
            else
            {
                return null;
            }
        }

        public bool IsTileAtPositionSame(IntVector2 position, Tile tile)
        {
            var instance = GetTileAtPosition(position * 32);

            return instance != null && instance.tile == tile;
        }

        private int NeighboursToIndex(bool a, bool b, bool c)
        {
            //return Random.Range(0, 5);

            return MinitileLookup((a ? 1 : 0) << 2 
                                | (b ? 1 : 0) << 1 
                                | (c ? 1 : 0)); 
        }

        private int MinitileLookup(int index)
        {
            switch (index)
            {
            case 0:
            case 1:
                return 0;
            case 2:
            case 3:
                return 1;
            case 4:
            case 5:
                return 2;
            case 6:
                return 3;
            case 7:
            default:
                return 4; 
            }
        }

        public void SetTileAtPosition(IntVector2 position, Tile tile)
        {
            if (tile == null)
            {
                tiles.Remove(position);
            }
            else
            {
                tiles[position] = new TileInstance
                {
                    tile = tile,
                };
            }

            for (int y = -1; y <= 1; ++y)
            {
                for (int x = -1; x <= 1; ++x)
                {
                    var coord = position + new IntVector2(x, y);

                    RefreshMinitiles(coord);
                }
            }
        }

        public void RefreshMinitiles(IntVector2 position)
        {
            TileInstance instance;

            if (!tiles.TryGetValue(position, out instance))
                return;

            var tile = instance.tile;

            //if (!tile.autotile)
            //    return;

            bool l = IsTileAtPositionSame(position + IntVector2.left,  tile);
            bool r = IsTileAtPositionSame(position + IntVector2.right, tile);
            bool u = IsTileAtPositionSame(position + IntVector2.up,    tile);
            bool d = IsTileAtPositionSame(position + IntVector2.down,  tile);

            bool lu = IsTileAtPositionSame(position + IntVector2.left  + IntVector2.up,   tile);
            bool ld = IsTileAtPositionSame(position + IntVector2.left  + IntVector2.down, tile);
            bool ru = IsTileAtPositionSame(position + IntVector2.right + IntVector2.up,   tile);
            bool rd = IsTileAtPositionSame(position + IntVector2.right + IntVector2.down, tile);

            int x0y0 = NeighboursToIndex(d, l, ld);
            int x1y0 = NeighboursToIndex(d, r, rd) +  5;
            int x0y1 = NeighboursToIndex(u, l, lu) + 10;
            int x1y1 = NeighboursToIndex(u, r, ru) + 15;

            //Debug.LogFormat("L, U, LU: {0}, {1}, {2}", l, u, lu);

            instance.minitiles = new List<int> { x0y0, x1y0, x0y1, x1y1 };
        }

        public void Blend(Changes changes, 
                          ManagedSprite<byte> sprite8, 
                          IntVector2 brushPosition, 
                          Blend<byte> blend)
        {
            // TODO: sprite8 + position => world rect
            foreach (var pair in tiles)
            {
                var instance = pair.Value;
                var tile = instance.tile;

                if (!instance.tile.autotile)
                {
                    instance.tile.singular.Blend(sprite8, blend, pair.Key * 32, brushPosition);

                    var change = changes.GetChange(instance.tile.singular, () => new KoolSpriteChange(instance.tile.singular));
                    changes.sprites.Add(instance.tile.singular);
                }
                else
                {
                    KoolSprite x0y0 = tile.minitiles[instance.minitiles[0]];
                    KoolSprite x1y0 = tile.minitiles[instance.minitiles[1]];
                    KoolSprite x0y1 = tile.minitiles[instance.minitiles[2]];
                    KoolSprite x1y1 = tile.minitiles[instance.minitiles[3]];

                    x0y0.Blend(sprite8, blend, (pair.Key * 32).Moved(0, 0), brushPosition);
                    x1y0.Blend(sprite8, blend, (pair.Key * 32).Moved(16, 0), brushPosition);
                    x0y1.Blend(sprite8, blend, (pair.Key * 32).Moved(0, 16), brushPosition);
                    x1y1.Blend(sprite8, blend, (pair.Key * 32).Moved(16, 16), brushPosition);

                    changes.GetChange(x0y0, () => new KoolSpriteChange(x0y0));
                    changes.GetChange(x1y0, () => new KoolSpriteChange(x1y0));
                    changes.GetChange(x0y1, () => new KoolSpriteChange(x0y1));
                    changes.GetChange(x1y1, () => new KoolSpriteChange(x1y1));
                    changes.sprites.Add(x0y0);
                    changes.sprites.Add(x1y0);
                    changes.sprites.Add(x0y1);
                    changes.sprites.Add(x1y1);
                }

                changes.ApplyTextures();
            }
        }
    }
}
