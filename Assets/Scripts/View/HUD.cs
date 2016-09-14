using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class HUD : MonoBehaviour 
{
    [SerializeField] private Animator animator;

    public enum Mode
    {
        Switch,
        Draw,
    }

    private Mode mode_;
    public Mode mode
    {
        set
        {
            mode_ = value;

            animator.SetInteger("Mode", (int) mode_);
        }

        get
        {
            return mode_;
        }
    }
}
