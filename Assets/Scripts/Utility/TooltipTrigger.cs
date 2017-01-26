using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class TooltipTrigger : MonoBehaviour 
{
    [SerializeField] private RectTransform source;
    [SerializeField] private TooltipBias bias;
    [SerializeField] private Tooltip tooltip;
    [Multiline]
    [SerializeField] private string text;
    
    private UIHover hover;

    private void Awake()
    {
        hover = gameObject.AddComponent<UIHover>();
        hover.triggerTime = 0.25f;

        hover.onHoverBegin.AddListener(() => tooltip.SetText(text));
        hover.onTriggerBegin.AddListener(() => tooltip.Show(source, text, bias));
        hover.onTriggerEnd.AddListener(() => tooltip.Hide());
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) hover.Cancel();
    }
}
