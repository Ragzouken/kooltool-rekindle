using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ViewComponent<TModel> : MonoBehaviour 
{
    public TModel model { get; protected set; }
}
