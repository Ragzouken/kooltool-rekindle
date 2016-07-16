using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.EventSystems;

public class ColourPanel : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [SerializeField] private Main main;
    [SerializeField] private PalettePanel palette;

    [SerializeField] private Slider slider;
    [SerializeField] private RectTransform cursor;
    [SerializeField] private Image field;

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        var rtrans = transform as RectTransform;
        Vector2 local;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rtrans, eventData.position, eventData.enterEventCamera, out local);

        cursor.anchoredPosition = local;

        local.x /= rtrans.rect.width;
        local.y /= rtrans.rect.height;

        main.EditPalette(palette.selected, Color.HSVToRGB(local.x, local.y, slider.value));
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        (this as IDragHandler).OnDrag(eventData);
    }
}
