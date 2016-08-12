using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

public static class ChrisMode
{
    [MenuItem("CONTEXT/Component/Save Preset", priority = 1000000)]
    private static void SavePreset(MenuCommand command)
    {
        var component = command.context as Component;

        string name = component.gameObject.name;
        string type = component.GetType().Name;

        AssetDatabase.CreateFolder("Assets", "Editor Presets");
        AssetDatabase.CreateFolder("Assets/Editor Presets", type);
        string path = "Assets/Editor Presets/" + type + "/" + name + ".prefab";

        AssetDatabase.DeleteAsset(path);

        var instance = new GameObject(name);

        UnityEditorInternal.ComponentUtility.CopyComponent(component);
        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(instance);

        PrefabUtility.CreatePrefab(path, instance);
        Object.DestroyImmediate(instance);
    }
}
