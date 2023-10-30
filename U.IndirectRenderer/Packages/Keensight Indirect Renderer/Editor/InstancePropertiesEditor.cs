#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Keensight.Rendering.Configs;

namespace Keensight.Rendering.Editor
{
    [CustomEditor(typeof(MeshProperties))]
    public class InstancePropertiesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var properties = target as MeshProperties;
            var containerField = serializedObject.FindProperty("Container");
                
            GUI.enabled = false;
            EditorGUILayout.PropertyField(containerField, true);
            GUI.enabled = true;

            base.OnInspectorGUI();
            if (serializedObject.ApplyModifiedProperties() || GUI.changed)
            {
                var prefabField = serializedObject.FindProperty("Prefab");
                if (prefabField.objectReferenceValue != null)
                {
                    properties.name = prefabField.objectReferenceValue.name;
                    EditorUtility.SetDirty(properties);
                    AssetDatabase.SaveAssets();
                }
            }
                
            EditorGUILayout.Space(10);
            if (!GUILayout.Button("Delete Properties Assets")) return;
            
            var container = containerField.objectReferenceValue as MeshesCollection;
            container.RemoveSelectedInstance(properties);
        }
    }
}

#endif