using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using Text = TMPro.TextMeshProUGUI;

public class NameInputPopup : MonoBehaviour 
{
    [SerializeField]
    private Text titleText;
    [SerializeField]
    private TMPro.TMP_InputField inputText;
    [SerializeField]
    private Button confirmButton;
    [SerializeField]
    private Button cancelButton;

    private Action<string> onConfirm;
    private Action onCancel;

    private void Start()
    {
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Cancel);
    }

    public void Confirm()
    {
        Close();
        onConfirm(inputText.text);
    }

    public void Cancel()
    {
        Close();
        onCancel();
    }

    public void Setup(string title,
                      string text,
                      Action<string> confirm,
                      Action cancel=null)
    {
        titleText.text = title;
        inputText.text = text;

        onConfirm = confirm;
        onCancel = cancel ?? delegate
        { };
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
