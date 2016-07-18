using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class BrushToggle : MonoBehaviour 
{
    [SerializeField] private Main main;

    [SerializeField] private Toggle toggle;
    [SerializeField] private Image thumbnail;

    private Main.Stamp stamp;

    private void Start()
    {
        toggle.onValueChanged.AddListener(active => { if (active) main.SetStamp(stamp); });
    }

    public void SetStamp(Main.Stamp stamp)
    {
        this.stamp = stamp;

        thumbnail.sprite = stamp.thumbnail;
    }
}
