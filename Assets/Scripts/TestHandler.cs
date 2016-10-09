using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class TestHandler : MonoBehaviour 
{
    private void Start()
    {
        var handler = new MessageHandler();

        handler.SetHandler<int>(number => Debug.Log("it's an int"));
        handler.SetHandler<float>(number => Debug.Log("it's a float"));

        handler.Handle(1);
        handler.Handle(1f);
        handler.Handle(2);
    }
}
