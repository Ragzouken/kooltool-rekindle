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
    public class TileMap
    {
        [JsonConverter(typeof(DictionarySerializer<IntVector2, Tile, Data>))]
        public class Data : Dictionary<IntVector2, Tile> { }

        public Data tiles = new Data();
    }
}
