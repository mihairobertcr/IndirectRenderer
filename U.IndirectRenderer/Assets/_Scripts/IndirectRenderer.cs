using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndirectRenderer : IDisposable
{
    public const int NUMBER_OF_ARGS_PER_INSTANCE_TYPE = NUMBER_OF_DRAW_CALLS * NUMBER_OF_ARGS_PER_DRAW;  // 3draws * 5args = 15args

    private const int NUMBER_OF_DRAW_CALLS = 3;                                                           // (LOD00 + LOD01 + LOD02)
    private const int NUMBER_OF_ARGS_PER_DRAW = 5;                                                        // (indexCount, instanceCount, startIndex, baseVertex, startInstance)
    private const int ARGS_BYTE_SIZE_PER_DRAW_CALL = NUMBER_OF_ARGS_PER_DRAW * sizeof(uint);              // 5args * 4bytes = 20 bytes

    private readonly IndirectRendererConfig _config;
    private readonly MeshProperties _meshProperties;
    private readonly uint[] _args;
    
    private MatricesInitializer _matricesInitializer;
    private LodBitonicSorter _lodBitonicSorter;

    private int _numberOfInstances = 16384;
    private int _numberOfInstanceTypes = 1;
    private Vector3 _cameraPosition = Vector3.zero;

    public IndirectRenderer(IndirectRendererConfig config, 
        List<Vector3> positions, 
        List<Vector3> rotations, 
        List<Vector3> scales)
    {
        _config = config;
        
        _meshProperties = CreateMeshProperties();
        _args = InitializeArgumentsBuffer();
        
        ShaderKernels.Initialize(_config);

        // Note: Considering we have multiple types of meshes
        // I don't know how this is working right now but
        // it should preserve the sorting functionality
        var count = NUMBER_OF_ARGS_PER_INSTANCE_TYPE; // * _numberOfInstanceTypes
        ShaderBuffers.InstancesArgsBuffer = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.IndirectArguments);
        ShaderBuffers.InstancesArgsBuffer.SetData(_args);
        
        _matricesInitializer = new MatricesInitializer(_config.MatricesInitializer, _meshProperties, _numberOfInstances);
        _matricesInitializer.Initialize(positions, rotations, scales);
        _matricesInitializer.Dispatch();

        _cameraPosition = _config.RenderCamera.transform.position;

        _lodBitonicSorter = new LodBitonicSorter(_config.LodBitonicSorter, _numberOfInstances);
        _lodBitonicSorter.Initialize(positions, _cameraPosition);

        _matricesInitializer.LogInstanceDrawMatrices();
        
        // TODO: OnPreCull
        _cameraPosition = _config.RenderCamera.transform.position;
        _lodBitonicSorter.Dispatch();

        _lodBitonicSorter.LogSortingData();
    }

    public void Dispose()
    {
        ShaderBuffers.Dispose();
    }

    private MeshProperties CreateMeshProperties()
    {
        var properties = new MeshProperties
        {
            Mesh = new Mesh(),
            Material = _config.Material,
            
            Lod0Vertices = (uint)_config.Lod0Mesh.vertexCount,
            Lod1Vertices = (uint)_config.Lod1Mesh.vertexCount,
            Lod2Vertices = (uint)_config.Lod2Mesh.vertexCount,
        
            Lod0Indices = _config.Lod0Mesh.GetIndexCount(0),
            Lod1Indices = _config.Lod1Mesh.GetIndexCount(0),
            Lod2Indices = _config.Lod2Mesh.GetIndexCount(0),
                
            Lod0PropertyBlock = new MaterialPropertyBlock(),
            Lod1PropertyBlock = new MaterialPropertyBlock(),
            Lod2PropertyBlock = new MaterialPropertyBlock(),
            ShadowLod0PropertyBlock = new MaterialPropertyBlock(),
            ShadowLod1PropertyBlock = new MaterialPropertyBlock(),
            ShadowLod2PropertyBlock = new MaterialPropertyBlock()
        };
        
        properties.Mesh = new Mesh();
        properties.Mesh.name = "Mesh"; // TODO: name it
        var meshes = new CombineInstance[]
        {
            new() { mesh = _config.Lod0Mesh },
            new() { mesh = _config.Lod1Mesh },
            new() { mesh = _config.Lod2Mesh }
        };
        
        properties.Mesh.CombineMeshes(
            combine: meshes,
            mergeSubMeshes: true,
            useMatrices: false,
            hasLightmapData: false);

        return properties;
    }

    private uint[] InitializeArgumentsBuffer()
    {
        // Argument buffer used by DrawMeshInstancedIndirect.
        // Buffer with arguments has to have five integer numbers
        // var args = new uint[5] { 0, 0, 0, 0, 0 };
        // args[0] = _config.Lod0Mesh.GetIndexCount(0);
        // args[1] = (uint)_numberOfInstances;
        // args[2] = _config.Lod0Mesh.GetIndexStart(0);
        // args[3] = _config.Lod0Mesh.GetBaseVertex(0);
        // var size = args.Length * sizeof(uint);
        
        var args = new uint[NUMBER_OF_ARGS_PER_INSTANCE_TYPE]; //new uint[_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE]

        // Lod 0
        args[0] = _meshProperties.Lod0Indices; // 0 - index count per instance, 
        args[1] = 1;                           // 1 - instance count
        args[2] = 0;                           // 2 - start index location
        args[3] = 0;                           // 3 - base vertex location
        args[4] = 0;                           // 4 - start instance location
        
        // Lod 1
        args[5] = _meshProperties.Lod1Indices; // 0 - index count per instance, 
        args[6] = 1;                           // 1 - instance count
        args[7] = args[0] + args[2];    // 2 - start index location
        args[8] = 0;                    // 3 - base vertex location
        args[9] = 0;                    // 4 - start instance location
        
        // Lod 2
        args[10] = _meshProperties.Lod2Indices;     // 0 - index count per instance, 
        args[11] = 1;                   // 1 - instance count
        args[12] = args[5] + args[7];   // 2 - start index location
        args[13] = 0;                   // 3 - base vertex location
        args[14] = 0;                   // 4 - start instance location

        return args;
    }
}
