using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public interface ICopyable<T>
{
    void Copy(Copier copier, T copy);
}

public class Copier : Dictionary<object, object>
{
    public static T EZCopy<T>(T original)
        where T : class, ICopyable<T>, new()
    {
        var copier = new Copier();

        return copier.Copy(original);
    }

    public T Copy<T>(T original)
        where T : class, ICopyable<T>, new()
    {
        // a null is a null!
        if (original == null) return null; 

        // if we already copied this, use the existing copy
        object copy;

        if (!TryGetValue(original, out copy))
        {
            // otherwise, construct a blank instance and save it
            copy = new T();

            this[original] = copy;

            // perform the copying after we've saved the new (incomplete) copy
            // this way any circular references to the currently copying object
            // can be resolved by just returning our incomplete copy
            original.Copy(this, (T) copy);
        }

        return (T) copy;
    }

    public T CopyFake<T>(T original)
    {
        object copy;

        if (!TryGetValue(original, out copy))
        {
            Debug.LogError("Don't know how to create copy of abstract type!");
        }

        return (T) copy;
    }
}
