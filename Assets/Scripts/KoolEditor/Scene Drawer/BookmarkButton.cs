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

public class BookmarkButton : InstanceView<Bookmark> 
{
    [SerializeField]
    private SceneToggle toggle;
    [SerializeField]
    private Text nameText;
    [SerializeField]
    private Button button;

    private void Start()
    {
        button.onClick.AddListener(() => toggle.OpenBookmark(config));
    }
}
