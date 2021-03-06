﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class BrushToggle : InstanceView<Main.Stamp> 
{
    [SerializeField] private Main main;

    [SerializeField] private Toggle toggle;
    [SerializeField] private Image thumbnail;

    private void Start()
    {
        toggle.onValueChanged.AddListener(active => { if (active) main.SetStamp(config); });
    }

    protected override void Configure()
    {
        thumbnail.sprite = config.thumbnail;
    }
}
