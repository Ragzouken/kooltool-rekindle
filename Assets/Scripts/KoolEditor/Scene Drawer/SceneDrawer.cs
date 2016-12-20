using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

using kooltool;

public class SceneDrawer : MonoBehaviour 
{
    [SerializeField]
    private KoolEditor editor;

    [SerializeField]
    private GameObject TESTBlocker;

    [SerializeField]
    private Animator animator;
    [SerializeField]
    private InstancePoolSetup scenesSetup;
    private InstancePool<Scene, SceneToggle> scenes;

    public bool open
    {
        set
        {
            animator.SetBool("Open", value);

            if (value)
            {
                Refresh();
            }

            TESTBlocker.SetActive(value);
        }

        get
        {
            return animator.GetBool("Open");
        }
    }

    private void Start()
    {
        scenes = scenesSetup.FinaliseMono<Scene, SceneToggle>();
    }

    public void Refresh()
    {
        scenes.SetActive(editor.project.scenes);
        scenes.Get(editor.main.editScene).selected = true;
    }

    public void ToggleOpen()
    {
        open = !open;
    }
}
