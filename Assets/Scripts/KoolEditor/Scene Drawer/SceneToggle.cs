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

    [SerializeField]
    private InstancePoolSetup bookmarksSetup;
    private InstancePool<Bookmark, BookmarkButton> bookmarks;

    public bool selected
    {
        set
        {
            toggle.isOn = value;
        }
    }

    private void Awake()
    {
        clicks.onSingleClick.AddListener(() => editor.SwitchActiveScene(config));

        bookmarks = bookmarksSetup.FinaliseMono<Bookmark, BookmarkButton>();
    }

    public override void Refresh()
    {
        nameText.text = config.name;

        bookmarks.SetActive(config.bookmarks);
    }

    public void OpenBookmark(Bookmark bookmark)
    {
        selected = true;
        editor.SwitchActiveScene(config);
        editor.MoveToBookmark(bookmark);
    }
}
