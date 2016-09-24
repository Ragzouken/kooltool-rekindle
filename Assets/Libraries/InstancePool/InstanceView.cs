using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public abstract class InstanceView<TConfig> : MonoBehaviour
{
    public TConfig config { get; private set; }

    public void SetConfig(TConfig config)
    {
        this.config = config;

        Configure();
    }

    protected virtual void Configure() { Refresh(); }
    public virtual void Cleanup() { }
    public virtual void Refresh() { }
}
