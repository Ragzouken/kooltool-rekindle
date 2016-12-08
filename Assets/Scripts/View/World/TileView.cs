using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class TileView : InstanceView<IntVector2>
{
    [SerializeField]
    private SpriteRenderer fullRenderer;
    [SerializeField]
    private SpriteRenderer[] subRenderers;
    [SerializeField]
    private TileMapView tilemap;

    protected override void Configure()
    {
        transform.localPosition = config * 32;

        Refresh();
    }

    public override void Refresh()
    {
        var instance = tilemap.config.tiles[config];
        var tile = instance.tile;

        if (tile.autotile)
        {
            fullRenderer.enabled = false;

            for (int i = 0; i < 4; ++i)
            {
                int mini = instance.minitiles[i];

                subRenderers[i].enabled = true;
                subRenderers[i].sprite = tile.minitiles[mini].uSprite;
            }
        }
        else
        {
            fullRenderer.enabled = true;
            fullRenderer.sprite = tile.singular.uSprite;

            for (int i = 0; i < 4; ++i)
            {
                subRenderers[i].enabled = false;
            }
        }
    }
}
