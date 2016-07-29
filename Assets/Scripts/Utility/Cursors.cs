using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Cursors : ScriptableObjectSingleton<Cursors>, ISerializationCallbackReceiver
{
    public enum Type
    {
        None,
        Missing,

        Normal,
        Press,
    }

    [System.Serializable]
    public class Cursor
    {
        public Texture2D texture;
        public Vector2 hotspot;
    }

    [HideInInspector][SerializeField] private List<string> cursorsKeys;
    [HideInInspector][SerializeField] private List<Cursor> cursorsValues;

    public class CursorListing : Dictionary<Type, Cursor> { }
        
    public CursorListing cursors = new CursorListing();

    public Cursor this[Type icon]
    {
        get
        {
            return cursors.ContainsKey(icon) ? cursors[icon] : cursors[Type.Missing];
        }
    }

    public static void Set(Type cursor)
    {
        Texture2D texture = Instance[cursor].texture;
        Vector2 hotspot = Instance[cursor].hotspot;
        //hotspot.x /= texture.width;
        //hotspot.y /= texture.height;

        UnityEngine.Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        cursors.Clear();

        for (int i = 0; i < cursorsKeys.Count; ++i)
        {
            string name = cursorsKeys[i];
            var icon = (Type) System.Enum.Parse(typeof(Type), name);

            cursors.Add(icon, cursorsValues[i]);
        }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        cursorsKeys.Clear();
        cursorsValues.Clear();

        foreach (var pair in cursors)
        {
            cursorsKeys.Add(pair.Key.ToString());
            cursorsValues.Add(pair.Value);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Cursors))]
public class CursorsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var cursors = (target as Cursors).cursors;

        foreach (var cursor in System.Enum.GetValues(typeof(Cursors.Type)).Cast<Cursors.Type>())
        {
            if (!cursors.ContainsKey(cursor)) cursors[cursor] = new Cursors.Cursor();

            cursors[cursor].texture = EditorGUILayout.ObjectField(cursor.ToString(),
                                                                  cursors[cursor].texture,
                                                                  typeof(Texture2D), 
                                                                  false) as Texture2D;
            cursors[cursor].hotspot = EditorGUILayout.Vector2Field("Hotspot",
                                                                   cursors[cursor].hotspot);
        }

        EditorUtility.SetDirty(target);
    }
}
#endif
