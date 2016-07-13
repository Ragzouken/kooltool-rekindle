using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class PalettePanel : MonoBehaviour 
{
    [SerializeField] private Toggle[] colorToggles;
    [SerializeField] private Image[] colorImages;
    [SerializeField] private ToggleGroup group;

    public event System.Action<int> OnPaletteIndexSelected = delegate { };

    public int selected { get; private set; }

    private World world;

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

        OnPaletteIndexSelected(selected);
    }
}
