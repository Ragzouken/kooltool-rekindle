using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler 
{
    [Serializable]
    public class OnAction : UnityEvent { };

    public bool hovering { get; private set; }
    public bool triggered { get; private set; }

    public float triggerTime;

    public OnAction onHoverBegin = new OnAction();
    public OnAction onTrigger = new OnAction();
    public OnAction onHoverEnd = new OnAction();

    private float triggerTimer;

    private void Update()
    {
        if (!triggered && hovering)
        {
            triggerTimer += Time.unscaledDeltaTime;

            if (triggerTimer > triggerTime)
            {
                triggered = true;
                onTrigger.Invoke();
            }
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        triggered = false;
        triggerTimer = 0;

        onHoverBegin.Invoke();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        triggered = false;
        triggerTimer = 0;

        onHoverEnd.Invoke();
    }
}
