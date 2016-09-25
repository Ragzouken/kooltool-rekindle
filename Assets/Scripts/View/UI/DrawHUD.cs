using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class DrawHUD : MonoBehaviour 
{
    public enum Mode
    {
        Paint,
        Colors,
        Select,
    }

    [SerializeField] private Main main;

    [SerializeField] private Animator animator;

    [SerializeField] private Toggle[] colorToggles;
    [SerializeField] private Image[] colorImages;
    [SerializeField] private ToggleGroup group;

    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider2D hueSaturationSlider;

    [SerializeField] private Tooltip tooltip;

    public event System.Action<int> OnPaletteIndexSelected = delegate { };

    public Mode mode { get; private set; }
    public int selected { get; private set; }

    private World world;

    private void Awake()
    {
        brightnessSlider.onValueChanged.AddListener(value => UpdateColourFromUI(false));
        hueSaturationSlider.onUserChangedValue.AddListener(value => UpdateColourFromUI(false));

        hueSaturationSlider.onUserChangedValueFull.AddListener((prev, next) =>
        {
            inside = false;

            main.RecordPaletteHistory(selected, original, main.project.world.palette[selected]);
        });

        for (int i = 0; i < colorToggles.Length; ++i)
        {
            var toggle = colorToggles[i];
            int index = i;

            var clicks = toggle.gameObject.AddComponent<UIClicks>();

            clicks.onSingleClick.AddListener(() => SelectPaletteIndex(index));
            clicks.onDoubleClick.AddListener(() => SetMode(Mode.Colors));

            /*
            var hover = toggle.gameObject.AddComponent<UIHover>();

            hover.onTrigger.AddListener(() => tooltip.Show(toggle.transform as RectTransform, 
                                                           "Draw in this colour, double click to edit the color"));
            hover.onHoverEnd.AddListener(() => tooltip.Hide());
            hover.triggerTime = 0.25f;
            */
        }
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
            original = main.project.world.palette[selected];
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
        }

        colorToggles[0].isOn = true;
    }

    public void SetModePaint()  { SetMode(Mode.Paint); }
    public void SetModeColors() { SetMode(Mode.Colors); }
    public void SetModeSelect() { SetMode(Mode.Select); }

    public void SetMode(Mode mode)
    {
        this.mode = mode;

        animator.SetInteger("Mode", (int) mode);
        if (mode != Mode.Paint)
            expanded = false;
    }

    public bool expanded
    {
        set
        {
            animator.SetBool("Expand Brush", value);
        }

        get
        {
            return animator.GetBool("Expand Brush");
        }
    }

    public void ToggleExpanded()
    {
        expanded = !expanded;
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
