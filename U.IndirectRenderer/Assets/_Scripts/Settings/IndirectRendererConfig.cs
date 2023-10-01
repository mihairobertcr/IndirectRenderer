using System;
using UnityEngine;

[Serializable]
public class IndirectRendererConfig
{
    [Header("Rendering")]
    public Camera Camera;

    [Header("Compute Shaders")]
    public ComputeShader MatricesInitializer;
    public ComputeShader LodBitonicSorter;
    public ComputeShader InstancesCuller;
    public ComputeShader InstancesScanner;
    public ComputeShader GroupSumsScanner;
    public ComputeShader InstancesDataCopier;
}