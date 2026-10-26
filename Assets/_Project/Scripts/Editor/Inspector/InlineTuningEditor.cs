using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Inspector
{
    public abstract class InlineTuningEditor : UnityEditor.Editor
    {
        readonly List<UnityEngine.Object> cachedAssets = new List<UnityEngine.Object>();
        readonly List<UnityEditor.Editor> cachedEditors = new List<UnityEditor.Editor>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
            DrawTuningReferences();
        }

        void DrawTuningReferences()
        {
            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;

                Type fieldType = FindFieldType(iterator.propertyPath);
                // Olay kanalları da ScriptableObject; yalnız *Tuning tipleri gömülür.
                if (fieldType == null || !typeof(ScriptableObject).IsAssignableFrom(fieldType)
                    || !fieldType.Name.EndsWith("Tuning", StringComparison.Ordinal)) continue;

                EditorGUILayout.Space(4f);
                if (iterator.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox("Seçili nesnelerde bu asset alanı farklı.", MessageType.Info);
                    continue;
                }
                UnityEngine.Object asset = iterator.objectReferenceValue;
                if (asset == null)
                {
                    EditorGUILayout.HelpBox("Tuning asset'i atanmamış.", MessageType.Warning);
                    if (GUILayout.Button("Oluştur ve ata")) CreateAndAssign(iterator, fieldType);
                    continue;
                }

                string key = "Game.InlineTuning." + target.GetType().FullName + "." + iterator.propertyPath;
                bool expanded = EditorPrefs.GetBool(key, true);
                expanded = EditorGUILayout.Foldout(expanded, "Ayarlar — " + asset.name, true, EditorStyles.foldoutHeader);
                EditorPrefs.SetBool(key, expanded);
                if (!expanded) continue;

                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Bu değerler asset'te durur; aynı asset'i kullanan her şey etkilenir. Play modunda yaptığın değişiklik çıkınca KALIR.", MessageType.None);
                GetCachedEditor(asset)?.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }

        Type FindFieldType(string path)
        {
            string fieldName = path;
            int dot = fieldName.LastIndexOf('.');
            if (dot >= 0) fieldName = fieldName.Substring(dot + 1);
            return target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.FieldType;
        }

        void CreateAndAssign(SerializedProperty property, Type assetType)
        {
            string folder = "Assets/_Project/Data/Tuning";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("Tuning klasörü bulunamadı", folder, "Tamam");
                return;
            }
            var created = ScriptableObject.CreateInstance(assetType);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetType.Name + ".asset");
            AssetDatabase.CreateAsset(created, path);
            Undo.RecordObjects(serializedObject.targetObjects, "Tuning asset oluştur");
            property.objectReferenceValue = created;
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }


        UnityEditor.Editor GetCachedEditor(UnityEngine.Object asset)
        {
            int index = cachedAssets.IndexOf(asset);
            if (index >= 0) return cachedEditors[index];
            UnityEditor.Editor created = null;
            UnityEditor.Editor.CreateCachedEditor(asset, null, ref created);
            cachedAssets.Add(asset);
            cachedEditors.Add(created);
            return created;
        }

        void DestroyCachedEditor()
        {
            foreach (var editor in cachedEditors)
                if (editor != null) DestroyImmediate(editor);
            cachedEditors.Clear();
            cachedAssets.Clear();
        }

        protected virtual void OnDisable() => DestroyCachedEditor();
    }
}
