using System.Collections.Generic;
using UnityEngine;
using Keensight.Rendering.Data;

namespace Keensight.Rendering.Configs
{
    public class MeshProperties : ScriptableObject
    {
    #if UNITY_EDITOR
        [HideInInspector, SerializeField]
        internal MeshesCollection Container;
    #endif

        [Header("Rendering"), Space(5)] 
        public GameObject Prefab;
        public Material Material;
        
        [Space, Header("Mesh"), Space(5)]
        public Mesh CombinedMesh;
        public uint DefaultLod;
        public bool RecombineLods;
        public List<LodProperties> Lods = new();

        [Space, Header("Location"), Space(5)]
        public TransformDto Offset;
        public Bounds Bounds;
        
        [Space(5)]
        public List<TransformDto> Transforms = new();

        public void Initialize()
        {
            InitializeCombinedMesh();
            InitializeMaterialPropertyBlocks();
        }

        private void InitializeCombinedMesh()
        {
            if (!RecombineLods) return;

            CombinedMesh = new Mesh();
            CombinedMesh.name = Prefab.name;

            var combinedMeshes = new CombineInstance[Lods.Count];
            for (var i = 0; i < Lods.Count; i++)
            {
                var lod = Lods[i];
                combinedMeshes[i] = new CombineInstance
                {
                    mesh = lod.Mesh
                };
            }

            CombinedMesh.CombineMeshes(
                combine: combinedMeshes,
                mergeSubMeshes: false,
                useMatrices: false,
                hasLightmapData: false);
            
            CombinedMesh.RecalculateTangents();
            CombinedMesh.RecalculateNormals();
        }

        private void InitializeMaterialPropertyBlocks()
        {
            foreach (var lod in Lods)
            {
                lod.Initialize();
            }
        }
    }
}
