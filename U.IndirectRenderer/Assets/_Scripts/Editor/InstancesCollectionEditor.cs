#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InstancesCollection))]
public class InstancesCollectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var collection = target as InstancesCollection;
        var dataProperty = serializedObject.FindProperty("Data");
            
        GUI.enabled = false;
        EditorGUILayout.PropertyField(dataProperty, true);
        GUI.enabled = true;
        EditorGUILayout.Space(10);
            
        if (GUILayout.Button("Add Instance"))
        {
            collection.CreateNewInstance();
        }
    }
}

#endif