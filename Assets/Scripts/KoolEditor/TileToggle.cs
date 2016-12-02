using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class TileToggle : InstanceView<Tile> 
{
    [SerializeField]
    private TileHUD hud;

    [SerializeField]
    private UIClicks clicks;
    [SerializeField]
    private Toggle toggle;
    [SerializeField]
    private Image image;

    public bool selected
    {
        set
        {
            toggle.isOn = value;
        }
    }

    private void Start()
    {
        clicks.onSingleClick.AddListener(() => hud.SelectTile(config));
    }

    public override void Refresh()
    {
        image.sprite = config.sprites[0].uSprite;
    }
}
