using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class TileView : InstanceView<IntVector2>
{
    [SerializeField] new private SpriteRenderer renderer;
    [SerializeField] private TileMapView tilemap;

    protected override void Configure()
    {
        renderer.transform.localPosition = config * 32;
        renderer.sprite = tilemap.config.tiles[config].sprites[0].uSprite;
    }

    public override void Refresh()
    {
        renderer.sprite = tilemap.config.tiles[config].sprites[0].uSprite;
    }
}
