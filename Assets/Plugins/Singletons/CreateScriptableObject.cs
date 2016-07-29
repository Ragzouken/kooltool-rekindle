#if UNITY_EDITOR
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

namespace CreateScriptableObject
{
    public class Window : EditorWindow
    {
        private string folder;
        private string query = "";
        private Vector2 scroll;

        [MenuItem("Assets/Create/ScriptableObject")]
        public static void Create(MenuCommand command)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
            string folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path);

            var window = GetWindow<Window>(utility: true,
                                           title: "Create Scriptable Object");
            window.Open(folder);
        }

        public void Open(string folder)
        {
            this.folder = folder;

            Show();
        }

        private void OnGUI()
        {
            query = EditorGUILayout.TextField("Search", query);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.Where(assembly => assembly.FullName.Contains("Assembly-CSharp")).SelectMany(assembly => assembly.GetTypes());

            Debug.Log(string.Join("\n", assemblies.Select(a => a.ToString()).ToArray()));

            var valid = types.Where(type => type.IsSubclassOf(typeof(ScriptableObject)))
                             .Where(type => !type.IsGenericTypeDefinition);

            foreach (Type type in valid)
            {
                if ((query == "" || type.Name.Contains(query))
                 && GUILayout.Button(type.Name))
                {
                    Close();

                    var obj = CreateInstance(type);

                    string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + type.Name + ".asset");

                    AssetDatabase.CreateAsset(obj, path);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = obj;
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
