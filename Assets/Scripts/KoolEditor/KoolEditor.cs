using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using kooltool;

public class KoolEditor : MonoBehaviour 
{
    public Project project;

    [HideInInspector]
    public List<Tile> tilePalette = new List<Tile>();

    public Tile CreateNewTile()
    {
        // TODO: support undo
        // TODO: events?

        return project.CreateDynamicTile(false);
    }

    public void DeleteExistingTile(Tile tile)
    {
        // TODO: undo, events

        project.tiles.Remove(tile);
        tilePalette.Remove(tile);
    }

    public void TileSetAutotile(Tile tile, bool autotile)
    {
        tile.autotile = autotile;
    }
}
