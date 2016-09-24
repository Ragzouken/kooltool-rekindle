using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class InstancePool<TConfig, TInstance>
    where TInstance : InstanceView<TConfig>
{
    protected TInstance prefab;
    protected Transform parent;
    
    protected Dictionary<TConfig, TInstance> instances
        = new Dictionary<TConfig, TInstance>();
    protected List<TInstance> spare = new List<TInstance>();

    public InstancePool(TInstance prefab, Transform parent, bool sort=true) 
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public TInstance this[TConfig config]
    {
        get
        {
            return Get(config);
        }
    }

    protected TInstance New(TConfig config)
    {
        TInstance instance;

        if (spare.Count > 0)
        {
            instance = spare[spare.Count - 1];
            spare.RemoveAt(spare.Count - 1);
        }
        else
        {
            instance = Object.Instantiate(prefab);
        }

        Configure(config, instance);

        return instance;
    }

    public TInstance Get(TConfig config)
    {
        TInstance instance;

        if (!instances.TryGetValue(config, out instance))
        {
            instance = New(config);
        }

        return instance;
    }

    public bool Discard(TConfig config)
    {
        TInstance instance;

        if (instances.TryGetValue(config, out instance))
        {
            instances.Remove(config);
            spare.Add(instance);

            Cleanup(config, instance);

            return true;
        }

        return false;
    }

    public void Clear()
    {
        foreach (var pair in instances)
        {
            Cleanup(pair.Key, pair.Value);
        }

        spare.AddRange(instances.Values);
        instances.Clear();
    }

    protected virtual void Configure(TConfig config, TInstance instance)
    {
        instance.transform.SetParent(parent, false);
        instance.gameObject.SetActive(true);

        instances.Add(config, instance);

        instance.Configure(config);
    }

    protected virtual void Cleanup(TConfig config, TInstance instance)
    {
        instance.gameObject.SetActive(false);

        instance.Cleanup();
    }

    public void Refresh()
    {
        MapActive(i => i.Refresh());
    }

    public void SetActive(params TConfig[] configs)
    {
        SetActive(configs);
    }

    private bool locked;
    private HashSet<TConfig> setActiveTempSet = new HashSet<TConfig>();
    private List<TConfig> setActiveTempList = new List<TConfig>();

    public void SetActive(IEnumerable<TConfig> active, bool sort=true)
    {
        Assert.IsFalse(locked, "GC OPTIMISATION MEANS THESE CALLS CANNOT BE NESTED!!");

        locked = true;
        setActiveTempSet.Clear();
        setActiveTempList.Clear();

        var collection = setActiveTempSet;
        var existing = setActiveTempList;

        if (active.Any()) collection.UnionWith(active);
        existing.AddRange(instances.Keys);

        for (int i = 0; i < existing.Count; ++i)
        {
            TConfig config = setActiveTempList[i];

            if (!collection.Contains(config)) Discard(config);
        }

        locked = false;

        foreach (TConfig shortcut in active)
        {
            var instance = Get(shortcut);

            if (sort) instance.transform.SetAsLastSibling();
        }
    }

    public void MapActive(System.Action<TInstance> action)
    {
        foreach (TInstance instance in instances.Values)
        {
            action(instance);
        }
    }

    public bool IsActive(TConfig shortcut)
    {
        return instances.ContainsKey(shortcut);
    }

    public bool DoIfActive(TConfig shortcut, 
                           System.Action<TInstance> action)
    {
        TInstance instance;

        if (instances.TryGetValue(shortcut, out instance))
        {
            action(instance);

            return true;
        }
        else
        {
            return false;
        }
    }
}
