using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class TileHUD : MonoBehaviour 
{
    [SerializeField]
    private KoolEditor editor;
    [SerializeField]
    private ToggleGroup group;
    [SerializeField]
    private InstancePoolSetup paletteSetup;
    private InstancePool<Tile, TileToggle> palette;

    private Tile _selected;
    public Tile selected
    {
        get
        {
            return _selected;
        }

        set
        {
            _selected = value;
            group.SetAllTogglesOff();

            if (selected != null)
            {
                palette.Get(_selected).selected = true;
            }
        }
    }

    private void Awake()
    {
        palette = paletteSetup.FinaliseMono<Tile, TileToggle>();
    }

    public void SetPalette(List<Tile> palette)
    {
        this.palette.SetActive(palette);
    }

    public void SelectTile(Tile tile)
    {
        
    }
}
