using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public static partial class VectorExtensions
{
    public static void GridCoords(this Vector2 position,
                                  int cellSize,
                                  out Vector2 cell,
                                  out Vector2 local)
    {
        cell = new Vector2(Mathf.Floor(position.x / cellSize),
                           Mathf.Floor(position.y / cellSize));

        float ox = position.x % cellSize;
        float oy = position.y % cellSize;

        local = new Vector2(ox >= 0 ? ox : cellSize + ox,
                            oy >= 0 ? oy : cellSize + oy);
    }

    public static Vector2 CellCoords(this Vector2 position,
                                     int cellSize)
    {
        position.x = Mathf.Floor(position.x / cellSize);
        position.y = Mathf.Floor(position.y / cellSize);

        return position;
    }

    public static Vector2 OffsetCoords(this Vector2 position,
                                       int cellSize)
    {
        float ox = position.x % cellSize;
        float oy = position.y % cellSize;

        position.x = ox >= 0 ? ox : cellSize + ox;
        position.y = oy >= 0 ? oy : cellSize + oy;

        return position;
    }
}
