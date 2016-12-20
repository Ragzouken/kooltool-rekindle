using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using Text = TMPro.TextMeshProUGUI;

using kooltool;

public class SceneToggle : InstanceView<Scene>
{
    [SerializeField]
    private KoolEditor editor;

    [SerializeField]
    private Toggle toggle;
    [SerializeField]
    private Text nameText;
    [SerializeField]
    private UIClicks clicks;

    private void Start()
    {
        clicks.onSingleClick.AddListener(() => editor.SwitchActiveScene(config));
    }

    public override void Refresh()
    {
        nameText.text = config.name;
    }
}
