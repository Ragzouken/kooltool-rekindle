using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using kooltool;

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
    private InstancePoolSetup browserSetup;
    private InstancePool<Tile, TileToggle> browser;

    [SerializeField]
    private Toggle eraserTile;

    [Header("Hovered Tile")]
    [SerializeField]
    private Image hoverImage;
    [SerializeField]
    private RectTransform hoverTransform;
    [SerializeField]
    private RectTransform hoverBounds;

    [Header("Browser Controls")]
    [SerializeField]
    private Button createButton;
    [SerializeField]
    private Button deleteButton;
    [SerializeField]
    private Toggle autotileToggle;
    [SerializeField]
    private UIClicks autotileClicks;
    [SerializeField]
    private Toggle passableToggle;
    [SerializeField]
    private UIClicks passableClicks;

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

            RefreshSelected();
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
        browser = browserSetup.FinaliseMono<Tile, TileToggle>();
    }

    private void Start()
    {
        createButton.onClick.AddListener(OnCreateClicked);
        deleteButton.onClick.AddListener(OnDeleteClicked);
        passableClicks.onSingleClick.AddListener(OnPassableClicked);
        autotileClicks.onSingleClick.AddListener(OnAutotileClicked);
    }

    private void OnCreateClicked()
    {
        selected = editor.CreateNewTile();
    }

    private void OnDeleteClicked()
    {
        if (selected != null)
        {
            editor.DeleteExistingTile(selected);
            SetPalette(editor.tilePalette);
        }
    }

    private void OnPassableClicked()
    {

    }

    private void OnAutotileClicked()
    {
        if (selected != null)
        {
            editor.TileSetAutotile(selected, !selected.autotile);
        }
    }

    public void SetProject(Project project)
    {
        browser.SetActive(project.tiles);
    }

    public void SetPalette(List<Tile> palette)
    {
        this.palette.SetActive(palette);

        RefreshSelected();
    }

    public void HoverTile(TileToggle toggle)
    {
        var tile = toggle.config;

        hoverTransform.gameObject.SetActive(true);
        hoverImage.sprite = tile.thumbnail.uSprite;

        hoverTransform.position = toggle.transform.position;

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

    public void ToggleBrowserExpanded()
    {
        expanded = !expanded;
    }

    private void RefreshSelected()
    {
        group.SetAllTogglesOff();

        if (selected != null)
        {
            palette.DoIfActive(_selected, toggle => toggle.selected = true);
            browser.DoIfActive(_selected, toggle => toggle.selected = true);

            passableToggle.isOn = true;
            autotileToggle.isOn = selected.autotile;
        }
        else
        {
            eraserTile.isOn = true;
        }
    }
}
