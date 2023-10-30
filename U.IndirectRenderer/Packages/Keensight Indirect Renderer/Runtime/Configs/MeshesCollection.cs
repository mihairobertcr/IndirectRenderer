using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Keensight.Rendering.Configs
{
    [CreateAssetMenu(order = 1,
        menuName = "Indirect Renderer/MeshesCollection",
        fileName = "MeshesCollection")]
    public class MeshesCollection : ScriptableObject
    {
        public List<MeshProperties> Data;

        #if UNITY_EDITOR
        public void CreateNewInstance()
        {
            var instance = CreateInstance<MeshProperties>();
            instance.Container = this;
            Data.Insert(0, instance);
        
            AssetDatabase.AddObjectToAsset(instance, this);
            AssetDatabase.SaveAssets();
        
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(instance);
        }

        public void RemoveSelectedInstance(MeshProperties properties)
        {
            Data.Remove(properties);
            Undo.DestroyObjectImmediate(properties);
            AssetDatabase.SaveAssets();
        }
        #endif
    }   
}