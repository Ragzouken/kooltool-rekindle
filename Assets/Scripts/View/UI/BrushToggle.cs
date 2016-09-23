using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class BrushToggle : PoolerInstance<Main.Stamp> 
{
    [SerializeField] private Main main;

    [SerializeField] private Toggle toggle;
    [SerializeField] private Image thumbnail;

    private void Start()
    {
        toggle.onValueChanged.AddListener(active => { if (active) main.SetStamp(shortcut); });
    }

    public override void SetShortcut(Main.Stamp stamp)
    {
        base.SetShortcut(stamp);

        thumbnail.sprite = stamp.thumbnail;
    }
}
