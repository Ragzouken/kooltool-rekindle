﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class PalettePanel : MonoBehaviour 
{
    [SerializeField] private Main main;

    [SerializeField] private Toggle[] colorToggles;
    [SerializeField] private Image[] colorImages;
    [SerializeField] private ToggleGroup group;

    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider2D hueSaturationSlider;

    public event System.Action<int> OnPaletteIndexSelected = delegate { };

    public int selected { get; private set; }

    private World world;

    private void Awake()
    {
        brightnessSlider.onValueChanged.AddListener(value => UpdateColourFromUI(false));
        hueSaturationSlider.onUserChangedValue.AddListener(value => UpdateColourFromUI(false));

        hueSaturationSlider.onUserChangedValueFull.AddListener((prev, next) =>
        {
            inside = false;

            main.RecordPaletteHistory(selected, original, main.world.palette[selected]);
        });
    }

    private Color original;
    private bool inside;
    private bool ignoreUI;
    private void UpdateColourFromUI(bool undo)
    {
        if (ignoreUI) return;

        if (!inside)
        {
            inside = true;
            original = main.world.palette[selected];
        }

        main.EditPalette(selected, Color.HSVToRGB(hueSaturationSlider.value.x, 
                                                  hueSaturationSlider.value.y, 
                                                  brightnessSlider.value));
    }

    public void SetWorld(World world)
    {
        this.world = world;

        for (int i = 0; i < 16; ++i)
        {
            int index = i;

            colorImages[i].color = Color.red * i / 15f;
            colorToggles[i].onValueChanged.AddListener(active =>
            {
                if (active) SelectPaletteIndex(index);
            });
        }

        colorToggles[0].isOn = true;
    }

    public void SelectPaletteIndex(int index)
    {
        if (index != selected) colorToggles[index].isOn = true;

        selected = index;

        float h, s, v;

        Color.RGBToHSV(world.palette[index], out h, out s, out v);

        ignoreUI = true;
        hueSaturationSlider.value = new Vector2(h, s);
        brightnessSlider.value = v;
        ignoreUI = false;

        OnPaletteIndexSelected(selected);
    }
}