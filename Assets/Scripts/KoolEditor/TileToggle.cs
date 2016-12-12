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
    private UIHover hover;
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
        clicks.onSingleClick.AddListener(() => hud.selected = config);

        hover.onHoverBegin.AddListener(() => hud.HoverTile(this));
        hover.onHoverEnd.AddListener(() => hud.UnHoverTile());
    }

    public override void Refresh()
    {
        image.sprite = config.thumbnail.uSprite;
    }
}
