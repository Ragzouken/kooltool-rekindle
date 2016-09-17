using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class SliderNumber : MonoBehaviour 
{
    [SerializeField] private Text text;
    [SerializeField] private Slider slider;

    private void Awake()
    {
        text.text = slider.value.ToString();

        slider.onValueChanged.AddListener(value => text.text = value.ToString());
    }
}
