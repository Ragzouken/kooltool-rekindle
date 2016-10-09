using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Text = TMPro.TextMeshProUGUI;

public class DialogueBox : MonoBehaviour 
{
    [SerializeField]
    private Text text;

    public void Show(string text)
    {
        gameObject.SetActive(true);
        this.text.text = text;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
