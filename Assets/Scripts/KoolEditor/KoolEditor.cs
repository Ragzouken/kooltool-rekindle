﻿using UnityEngine;
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
    [SerializeField]
    public Main main;
    [SerializeField]
    private CameraController camera;

    public Project project;

    [SerializeField]
    private SceneDrawer sceneDrawer;
    [SerializeField]
    private TileHUD tiles;

    [HideInInspector]
    public List<Tile> tilePalette = new List<Tile>();

    public void SwitchActiveScene(Scene scene)
    {
        main.SetEditScene(scene);
    }

    public void MoveToBookmark(Bookmark bookmark)
    {
        camera.focusTarget = bookmark.position * 32;
    }

    public void ScenePlaceTile(Scene scene, IntVector2 cell, Tile tile)
    {
        scene.tilemap.SetTileAtPosition(cell, tile);

        if (tilePalette.Count == 0 || tilePalette[0] != tile)
        {
            tilePalette.Remove(tile);
            tilePalette.Insert(0, tile);
            tilePalette = tilePalette.Take(16).ToList();
            tiles.SetPalette(tilePalette);
        }
    }

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
