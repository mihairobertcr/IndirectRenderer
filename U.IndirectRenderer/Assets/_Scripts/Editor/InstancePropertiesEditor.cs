#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InstanceProperties))]
public class InstancePropertiesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var properties = target as InstanceProperties;
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
        if (!GUILayout.Button("Remove Selected Instance")) return;
        
        var container = containerField.objectReferenceValue as InstancesCollection;
        container.RemoveSelectedInstance(properties);
    }
}

#endif