using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class PoolerInstance<TShortcut> : MonoBehaviour
{
    public TShortcut shortcut { get; protected set; }

    public virtual void SetShortcut(TShortcut shorcut)
    {
        this.shortcut = shorcut;
    }

    public virtual void Refresh()
    {

    }
}

public class PoolerPro<TShortcut, TInstance> : MonoBehaviourPooler<TShortcut, TInstance> 
    where TInstance : PoolerInstance<TShortcut>
{
    public PoolerPro(TInstance prefab, Transform parent) 
        : base(prefab, parent)
    {
        base.Initialize = Initialise;
        base.Cleanup = Cleanup;
    }

    protected void Initialise(TShortcut shortcut, TInstance instance)
    {
        instance.SetShortcut(shortcut);
    }

    protected new void Cleanup(TShortcut shortcut, TInstance instance)
    {

    }

    public void Refresh()
    {
        MapActive((s, i) => i.Refresh());
    }
}
