using System;
using UnityEngine;

[Serializable]
public class LodProperty
{
    public Mesh Mesh;
    public uint Range;
    public bool CastsShadows;
    
    public MaterialPropertyBlock MeshPropertyBlock;
    public MaterialPropertyBlock ShadowPropertyBlock;

    public void Initialize()
    {
        MeshPropertyBlock = new MaterialPropertyBlock();
        ShadowPropertyBlock = new MaterialPropertyBlock();
    }
}