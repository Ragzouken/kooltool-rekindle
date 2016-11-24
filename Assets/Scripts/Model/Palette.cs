using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

namespace kooltool
{
    public class Palette
    {
        public List<Color> colors = new List<Color>(Enumerable.Repeat(default(Color), 16));
    }
}
