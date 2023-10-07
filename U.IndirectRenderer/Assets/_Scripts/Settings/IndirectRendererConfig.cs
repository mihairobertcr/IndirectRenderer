using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class IndirectRendererConfig
{
    [Header("Rendering")]
    public Camera RenderCamera;
    public PowersOfTwo NumberOfInstances;
    public int NumberOfLods;

    [Header("Compute Shaders")]
    public ComputeShader MatricesInitializer;
    public ComputeShader LodBitonicSorter;
    public ComputeShader InstancesCuller;
    public ComputeShader InstancesScanner;
    public ComputeShader GroupSumsScanner;
    public ComputeShader InstancesDataCopier;
}