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
    public bool RecombineLods;
    public List<LodProperty> Lods;
    
    // public Mesh Lod0Mesh;
    // public Mesh Lod1Mesh;
    // public Mesh Lod2Mesh;

    [Space] 
    [Header("Location")]
    public TransformDto Offset;
    public Bounds Bounds;

    [Space] 
    [Header("Instances")]
    public List<TransformDto> Transforms; //TODO: Consider making it an array
    
    // public MaterialPropertyBlock Lod0PropertyBlock;
    // public MaterialPropertyBlock Lod1PropertyBlock;
    // public MaterialPropertyBlock Lod2PropertyBlock;
    //
    // public MaterialPropertyBlock ShadowLod0PropertyBlock;
    // public MaterialPropertyBlock ShadowLod1PropertyBlock;
    // public MaterialPropertyBlock ShadowLod2PropertyBlock;

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
        // var combinedMeshes = new CombineInstance[]
        // {
        //     new() { mesh = Lod0Mesh },
        //     new() { mesh = Lod1Mesh },
        //     new() { mesh = Lod2Mesh }
        // };

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
        // Lod0PropertyBlock = new MaterialPropertyBlock();
        // Lod1PropertyBlock = new MaterialPropertyBlock();
        // Lod2PropertyBlock = new MaterialPropertyBlock();
        //
        // ShadowLod0PropertyBlock = new MaterialPropertyBlock();
        // ShadowLod1PropertyBlock = new MaterialPropertyBlock();
        // ShadowLod2PropertyBlock = new MaterialPropertyBlock();
        foreach (var lod in Lods)
        {
            lod.Initialize();
        }
    }
}

[Serializable]
public class LodProperty
{
    public Mesh Mesh;
    public MaterialPropertyBlock MeshPropertyBlock;
    public MaterialPropertyBlock ShadowPropertyBlock;

    public void Initialize()
    {
        MeshPropertyBlock = new MaterialPropertyBlock();
        ShadowPropertyBlock = new MaterialPropertyBlock();
    }
}