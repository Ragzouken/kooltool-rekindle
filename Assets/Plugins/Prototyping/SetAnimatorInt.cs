using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class SetAnimatorInt : MonoBehaviour 
{
    [SerializeField] private Animator animator;
    [SerializeField] private string parameter;
    [SerializeField] private int value;

    public void Invoke()
    {
        animator.SetInteger(parameter, value);
    }
}
