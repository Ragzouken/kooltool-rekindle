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
    [SerializeField] private bool value;

    private void Start()
    {
        value = animator.GetBool(parameter);
    }

    public void Toggle()
    {
        value = !value;

        Update();
    }

    private void Update()
    {
        animator.SetBool(parameter, value);
    }
}
