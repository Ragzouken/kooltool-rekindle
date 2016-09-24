using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class InstanceView<TConfig> : MonoBehaviour
{
    public TConfig config { get; protected set; }

    public virtual void Configure(TConfig config)
    {
        this.config = config;
    }

    public virtual void Cleanup()
    {

    }

    public virtual void Refresh()
    {

    }
}
