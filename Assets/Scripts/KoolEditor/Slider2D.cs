using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Slider2D : MonoBehaviour, 
                        IDragHandler, 
                        IPointerDownHandler, 
                        IPointerUpHandler
{
    [Serializable]
    public class Change : UnityEvent<Vector2> { }

    [Serializable]
    public class ChangeFull : UnityEvent<Vector2, Vector2> { }

    [SerializeField] private RectTransform indicator;

    public Change onUserChangedValue;
    public ChangeFull onUserChangedValueFull;

    private Vector2 _value;
    public Vector2 value
    {
        get
        {
            return _value;
        }

        set
        {
            value.x = Mathf.Clamp01(value.x);
            value.y = Mathf.Clamp01(value.y);
            _value = value;

            var ptrans = transform.parent as RectTransform;

            value.Scale(ptrans.rect.size);

            indicator.anchoredPosition = value;
        }
    }

    private Vector2 original;



    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        var rtrans = transform as RectTransform;
        Vector2 local;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rtrans, eventData.position, eventData.enterEventCamera, out local);

        local.x /= rtrans.rect.width;
        local.y /= rtrans.rect.height;

        value = local;

        onUserChangedValue.Invoke(value);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        original = value;

        (this as IDragHandler).OnDrag(eventData);
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        onUserChangedValueFull.Invoke(original, value);
    }
}
