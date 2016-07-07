using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class ToggleAnimatorBool : MonoBehaviour 
{
    [SerializeField] private Animator animator;
    [SerializeField] private string parameter;

    public void Toggle()
    {
        animator.SetBool(parameter, !animator.GetBool(parameter));
    }
}
