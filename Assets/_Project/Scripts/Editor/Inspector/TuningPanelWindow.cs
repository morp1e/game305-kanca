using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Inspector
{
    public sealed class TuningPanelWindow : EditorWindow
    {
        const string TuningFolder = "Assets/_Project/Data/Tuning";
        readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        UnityEditor.Editor assetEditor;
        ScriptableObject selected;
        Vector2 listScroll;
        Vector2 inspectorScroll;

        [MenuItem("Kanca/Ayar Paneli")]
        static void Open() => GetWindow<TuningPanelWindow>("Ayar Paneli");

        void OnEnable() => Refresh();

        void OnDisable()
        {
            if (assetEditor != null) DestroyImmediate(assetEditor);
        }

        void Refresh()
        {
            assets.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { TuningFolder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) assets.Add(asset);
            }
            assets.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(180f));
            if (GUILayout.Button("Yenile")) Refresh();
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            foreach (var asset in assets)
            {
                bool active = selected == asset;
                if (GUILayout.Toggle(active, asset.name, "Button") && !active) Select(asset);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            if (selected == null) EditorGUILayout.HelpBox("Düzenlemek için bir tuning asset'i seçin.", MessageType.Info);
            else
            {
                inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
                assetEditor?.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        void Select(ScriptableObject asset)
        {
            selected = asset;
            if (assetEditor != null) DestroyImmediate(assetEditor);
            assetEditor = UnityEditor.Editor.CreateEditor(selected);
        }
    }
}
