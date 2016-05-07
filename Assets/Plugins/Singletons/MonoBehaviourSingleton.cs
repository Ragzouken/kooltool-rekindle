using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class MonoBehaviourSingleton<T> : MonoBehaviour 
    where T : MonoBehaviourSingleton<T>
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance != null) return _instance;

            var objects = Object.FindObjectsOfType<T>();

            Assert.IsTrue(objects.Length > 0, string.Format("Could not find any instance of the {0} singleton in the scene!", typeof(T).Name));
            Assert.IsTrue(objects.Length < 2, string.Format("Found multiple instances of the {0} singleton in the scene!", typeof(T).Name));

            _instance = objects[0];

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        Assert.IsNull(_instance, string.Format("Existing instance of the {0} singleton in the scene!", typeof(T).Name));

        _instance = this as T;
    }
}
