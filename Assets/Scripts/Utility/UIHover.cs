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
    public OnAction onTriggerBegin = new OnAction();
    public OnAction onTriggerEnd = new OnAction();
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
                onTriggerBegin.Invoke();
            }
        }
    }

    public void Cancel()
    {
        Reset();

        hovering = false;
    }

    public void Reset()
    {
        triggerTimer = 0;

        if (triggered)
        {
            triggered = false;
            onTriggerEnd.Invoke();
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;

        Reset();
        
        onHoverBegin.Invoke();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        hovering = false;

        Reset();
        
        onHoverEnd.Invoke();
    }
}
