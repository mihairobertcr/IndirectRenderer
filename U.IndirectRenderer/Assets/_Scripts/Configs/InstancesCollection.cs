using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(order = 1,
    menuName = "Indirect Renderer/InstancesCollection",
    fileName = "InstancesCollection")]
public class InstancesCollection : ScriptableObject
{
    [CustomEditor(typeof(InstancesCollection))]
    public class Inspector : Editor
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
                Repaint();
            }
            
            if (GUILayout.Button("Remove Selected Instance"))
            {
                collection.RemoveSelectedInstance();
                Repaint();
            }
        }
    }

    public List<InstanceProperties> Data;

#if UNITY_EDITOR
    public void CreateNewInstance()
    {
        var instance = CreateInstance<InstanceProperties>();
        Data.Insert(0, instance);
        
        AssetDatabase.AddObjectToAsset(instance, this);
        AssetDatabase.SaveAssets();
        
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(instance);
    }

    public void RemoveSelectedInstance()
    {
        var selected = Selection.activeObject as InstanceProperties;
        if (selected is null || !Data.Contains(selected))
        {
            Debug.Log("An item belonging to this collection has to be selected in order to be removed!");
            return;
        }

        Data.Remove(selected);
        Undo.DestroyObjectImmediate(selected);
        AssetDatabase.SaveAssets();
    }
#endif
}