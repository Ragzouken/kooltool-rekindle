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
    private Animator animator;
    [SerializeField]
    private InstancePoolSetup paletteSetup;
    private InstancePool<Tile, TileToggle> palette;

    [SerializeField]
    private Toggle eraserTile;

    [Header("Hovered Tile")]
    [SerializeField]
    private Image hoverImage;
    [SerializeField]
    private RectTransform hoverTransform;
    [SerializeField]
    private RectTransform hoverBounds;

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
            else
            {
                eraserTile.isOn = true;
            }
        }
    }

    public bool expanded
    {
        set
        {
            animator.SetBool("Expand Browser", value);
        }

        get
        {
            return animator.GetBool("Expand Browser");
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

    public void HoverTile(Tile tile)
    {
        hoverTransform.gameObject.SetActive(true);
        hoverImage.sprite = tile.sprites[0].uSprite;

        hoverTransform.position = palette.Get(tile).transform.position;

        UIExtensions.BoundRectTransform(hoverTransform, hoverBounds);
    }

    public void UnHoverTile()
    {
        hoverTransform.gameObject.SetActive(false);
    }

    public void SelectEraser()
    {
        selected = null;
    }

    public void SelectTile(Tile tile)
    {
        
    }

    public void ToggleBrowserExpanded()
    {
        expanded = !expanded;
    }
}
