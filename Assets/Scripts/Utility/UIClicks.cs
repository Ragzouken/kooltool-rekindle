using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIClicks : MonoBehaviour, IPointerClickHandler 
{
    [System.Serializable]
    public class OnAction : UnityEvent { };

    public OnAction onSingleClick = new OnAction();
    public OnAction onDoubleClick = new OnAction();

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 1)
        {
            onSingleClick.Invoke();
        }
        else if (eventData.clickCount == 2)
        {
            onDoubleClick.Invoke();
        }
    }
}
