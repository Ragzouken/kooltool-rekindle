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
    public partial class TileMap : ICopyable<TileMap>
    {
        [JsonConverter(typeof(DictionarySerializer<IntVector2, Tile, Data>))]
        public class Data : Dictionary<IntVector2, Tile> { }

        public Data tiles = new Data();

        public void Copy(Copier copier, TileMap copy)
        {
            foreach (var pair in tiles)
            {
                copy.tiles.Add(pair.Key, pair.Value);
            }
        }
    }

    public partial class TileMap
    {
        public Tile GetTileAtPosition(IntVector2 position)
        {
            Tile tile;

            if (tiles.TryGetValue(position.CellCoords(32), out tile))
            {
                return tile;
            }
            else
            {
                return null;
            }
        }

        public void Blend(Changes changes, 
                          ManagedSprite<byte> sprite8, 
                          IntVector2 brushPosition, 
                          Blend<byte> blend)
        {
            // TODO: sprite8 + position => world rect
            foreach (var pair in tiles)
            {
                pair.Value.sprites[0].Blend(sprite8, blend, pair.Key * 32, brushPosition);
                pair.Value.sprites[0].mTexture.Apply();

                var change = changes.GetChange(pair.Value.sprites[0], () => new KoolSpriteChange(pair.Value.sprites[0]));
                changes.sprites.Add(pair.Value.sprites[0]);
            }
        }
    }
}
