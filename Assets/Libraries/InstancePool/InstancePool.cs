using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public abstract class InstancePool<TConfig, TInstance>
    where TInstance : IConfigView<TConfig>
{    
    protected Dictionary<TConfig, TInstance> instances
        = new Dictionary<TConfig, TInstance>();
    protected List<TInstance> spare = new List<TInstance>();

    protected abstract TInstance CreateNew();

    protected TInstance FindNew(TConfig config)
    {
        TInstance instance;

        if (spare.Count > 0)
        {
            instance = spare[spare.Count - 1];
            spare.RemoveAt(spare.Count - 1);
        }
        else
        {
            instance = CreateNew();
        }

        Configure(config, instance);

        return instance;
    }

    public TInstance Get(TConfig config)
    {
        TInstance instance;

        if (!instances.TryGetValue(config, out instance))
        {
            instance = FindNew(config);
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
        instances.Add(config, instance);
        instance.SetConfig(config);
    }

    protected virtual void Cleanup(TConfig config, TInstance instance)
    {
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

    public virtual void SetActive(IEnumerable<TConfig> active)
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
            Get(shortcut);
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

// TODO: thing to convert MonoBehaviour into IConfigView

    /*
public class AnonymousView<TConfig> : IConfigView<TConfig>
{
    public TConfig config { get; protected set; }

    protected Action DoConfigure;
    protected Action DoCleanup;
    protected Action DoRefresh;

    public AnonymousView(Action DoConfigure = null, 
                         Action DoCleanup = null,
                         Action DoRefresh = null)
    {
        this.DoConfigure = DoConfigure ?? delegate { };
        this.DoCleanup   = DoCleanup ?? delegate { };
        this.DoRefresh   = DoRefresh ?? delegate { };
    }

    public void SetConfig(TConfig config)
    {
        this.config = config;

        DoConfigure();
    }

    public void Cleanup()
    {
        DoCleanup();
    }

    public void Refresh()
    {
        DoRefresh();
    }

}

public class InstancePoolP<TConfig> : InstancePool<TConfig, AnonymousView<TConfig>>
{
    protected MonoBehaviour prefab;
    protected Transform parent;
    protected bool sort;

    protected Func<AnonymousView<TConfig>> Instantiate;

    public InstancePoolP(MonoBehaviour prefab, 
                         Transform parent, 
                         bool sort=true)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.sort = sort;
    }

    public void Finalise<TInstance>(TInstance prefab)
        where TInstance : MonoBehaviour
    {

    }

    protected override AnonymousView<TConfig> CreateNew()
    {
        return Instantiate();
    }
}
*/

[System.Serializable]
public class InstancePoolSetup
{
    public GameObject prefab;
    public Transform parent;

    public InstancePool<TConfig> Finalise<TConfig>(bool sort=true)
    {
        var prefab = this.prefab.GetComponent<InstanceView<TConfig>>();

        return new InstancePool<TConfig>(prefab, parent, sort: sort);
    }
}

public class InstancePool<TConfig> : InstancePool<TConfig, InstanceView<TConfig>>
{
    protected InstanceView<TConfig> prefab;
    protected Transform parent;
    protected bool sort;

    public InstancePool(InstanceView<TConfig> prefab, Transform parent, bool sort=true)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.sort = sort;
    }

    protected override InstanceView<TConfig> CreateNew()
    {
        return Object.Instantiate(prefab);
    }

    protected override void Configure(TConfig config, InstanceView<TConfig> instance)
    {
        instance.transform.SetParent(parent, false);
        instance.gameObject.SetActive(true);

        base.Configure(config, instance);
    }

    protected override void Cleanup(TConfig config, InstanceView<TConfig> instance)
    {
        instance.gameObject.SetActive(false);

        base.Cleanup(config, instance);
    }

    public override void SetActive(IEnumerable<TConfig> active)
    {
        base.SetActive(active);

        if (sort)
        {
            foreach (TConfig shortcut in active)
            {
                Get(shortcut).transform.SetAsLastSibling();
            }
        }
    }
}

