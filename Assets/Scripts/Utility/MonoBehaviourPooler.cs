using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MonoBehaviourPooler<TShortcut, TInstance> 
    where TInstance : Component
{
    public delegate void Process(TShortcut shortcut, TInstance instance);

    protected TInstance Prefab;
    protected Transform Parent;
    protected Process Initialize;
    protected Process Cleanup;

    protected Dictionary<TShortcut, TInstance> instances
        = new Dictionary<TShortcut, TInstance>();

    protected Stack<TInstance> spare
        = new Stack<TInstance>();

    
    public IEnumerable<TShortcut> Shortcuts
    {
        get
        {
            foreach (TShortcut shortcut in instances.Keys) yield return shortcut;
        }
    }

    public IEnumerable<TInstance> Instances
    {
        get
        {
            foreach (TInstance instance in instances.Values) yield return instance;
        }
    }

    public MonoBehaviourPooler(TInstance prefab,
                               Transform parent=null,
                               Process initialize=null,
                               Process cleanup=null)
    {
        Prefab = prefab;
        Parent = parent;
        Initialize = initialize ?? delegate { };
        Cleanup = cleanup ?? delegate { };
    }

    public IEnumerator Preload(int count, bool wake=false)
    {
        count -= spare.Count;

        for (int i = 0; i < count; ++i)
        {
            TInstance instance = Object.Instantiate(Prefab);
            instance.gameObject.SetActive(false);

            spare.Push(instance);

            yield return null;
        }
    }

    protected TInstance New(TShortcut shortcut)
    {
        TInstance instance;

        if (spare.Count > 0)
        {
            instance = spare.Pop();
        }
        else
        {
            instance = Object.Instantiate<TInstance>(Prefab);
        }
        
        instance.transform.SetParent(Parent, false);
        instance.gameObject.SetActive(true);
        instances.Add(shortcut, instance);

        Initialize(shortcut, instance);

        return instance;
    }

    public TInstance Get(TShortcut shortcut)
    {
        TInstance instance;

        if (!instances.TryGetValue(shortcut, out instance))
        {
            instance = New(shortcut);
        }

        return instance;
    }

    public bool Discard(TShortcut shortcut)
    {
        TInstance instance;

        if (instances.TryGetValue(shortcut, out instance))
        {
            instances.Remove(shortcut);
            spare.Push(instance);

            instance.gameObject.SetActive(false);

            Cleanup(shortcut, instance);

            return true;
        }

        return false;
    }

    public void Clear()
    {
        foreach (TShortcut shortcut in new List<TShortcut>(instances.Keys))
        {
            Discard(shortcut);
        }
    }

    public void SetActive(IEnumerable<TShortcut> active, bool sort=true)
    {
        var collection = new HashSet<TShortcut>(active);

        foreach (TShortcut shortcut in new List<TShortcut>(instances.Keys))
        {
            if (!collection.Contains(shortcut)) Discard(shortcut);
        }

        foreach (TShortcut shortcut in active)
        {
            var instance = Get(shortcut);

            if (sort) instance.transform.SetAsLastSibling();
        }
    }

    public void SetActive(TShortcut active)
    {
        foreach (TShortcut shortcut in new List<TShortcut>(instances.Keys))
        {
            if (!shortcut.Equals(active)) Discard(shortcut);
        }

        Get(active);
    }

    public void MapActive(System.Action<TShortcut, TInstance> action)
    {
        foreach (var pair in instances)
        {
            action(pair.Key, pair.Value);
        }
    }

    public void SetActive()
    {
        Clear();
    }

    public bool IsActive(TShortcut shortcut)
    {
        return instances.ContainsKey(shortcut);
    }

    public bool DoIfActive(TShortcut shortcut, 
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
