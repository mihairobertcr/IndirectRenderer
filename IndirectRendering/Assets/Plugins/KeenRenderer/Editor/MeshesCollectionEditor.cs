#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using Keensight.Rendering.Configs;

namespace Keensight.Rendering.Editor
{
    [CustomEditor(typeof(MeshCollection))]
    public class MeshesCollectionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var collection = target as MeshCollection;
            var dataProperty = serializedObject.FindProperty("Data");
                
            GUI.enabled = false;
            EditorGUILayout.PropertyField(dataProperty, true);
            GUI.enabled = true;
            EditorGUILayout.Space(10);
                
            if (GUILayout.Button("Add Properties Asset"))
            {
                collection.CreateNewInstance();
            }
        }
    }
}

#endif