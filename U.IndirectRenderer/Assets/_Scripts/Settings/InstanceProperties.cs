using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InstanceProperties
{
    [Serializable]
    public class TransformDto
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
    }
    
    [Header("Rendering")]
    public GameObject Prefab;
    public Material Material;

    [Space] 
    [Header("Mesh")]
    public Mesh CombinedMesh;
    public uint DefaultLod;
    public bool RecombineLods;
    public List<LodProperty> Lods;

    [Space] 
    [Header("Location")]
    public TransformDto Offset;
    public Bounds Bounds;

    [Space] 
    [Header("Instances")]
    public List<TransformDto> Transforms; //TODO: Consider making it an array

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