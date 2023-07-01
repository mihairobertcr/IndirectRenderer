using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class IndirectRenderer : IDisposable
{
    public const int NUMBER_OF_ARGS_PER_INSTANCE_TYPE = NUMBER_OF_DRAW_CALLS * NUMBER_OF_ARGS_PER_DRAW; // 3draws * 5args = 15args

    private const int NUMBER_OF_DRAW_CALLS = 3;                                                         // (LOD00 + LOD01 + LOD02)
    private const int NUMBER_OF_ARGS_PER_DRAW = 5;                                                      // (indexCount, instanceCount, startIndex, baseVertex, startInstance)
    private const int ARGS_BYTE_SIZE_PER_DRAW_CALL = NUMBER_OF_ARGS_PER_DRAW * sizeof(uint);            // 5args * 4bytes = 20 bytes

    private readonly IndirectRendererConfig _config;
    private readonly IndirectRendererSettings _settings;
    private readonly HiZBufferConfig _hiZBufferConfig;
    private readonly MeshProperties _meshProperties;
    private readonly uint[] _args;
    
    private readonly MatricesHandler _matricesHandler;
    private readonly LodBitonicSorter _lodBitonicSorter;
    private readonly Culler _culler;

    private int _numberOfInstances = 16384;
    private int _numberOfInstanceTypes = 1;
    
    private List<Vector3> _positions;
    private List<Vector3> _scales;
    private List<Vector3> _rotations;
    
    private Vector3 _cameraPosition = Vector3.zero;
    private Bounds _bounds;

    public IndirectRenderer(IndirectRendererConfig config, 
        IndirectRendererSettings settings, 
        HiZBufferConfig hiZBufferConfig,
        List<Vector3> positions, 
        List<Vector3> rotations, 
        List<Vector3> scales)
    {
        _config = config;
        _settings = settings;
        _hiZBufferConfig = hiZBufferConfig;
        
        _meshProperties = CreateMeshProperties();
        _args = InitializeArgumentsBuffer();
        
        ShaderKernels.Initialize(_config);

        // Note: Considering we have multiple types of meshes
        // I don't know how this is working right now but
        // it should preserve the sorting functionality
        var count = NUMBER_OF_ARGS_PER_INSTANCE_TYPE; // * _numberOfInstanceTypes
        ShaderBuffers.Args = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.IndirectArguments);
        ShaderBuffers.ShadowsArgs = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.IndirectArguments);
        ShaderBuffers.Args.SetData(_args);
        ShaderBuffers.ShadowsArgs.SetData(_args);

        _matricesHandler = new MatricesHandler(_config.MatricesInitializer, _numberOfInstances, _meshProperties);
        _culler = new Culler(_config.Culler, _numberOfInstances, _config.RenderCamera);
        _lodBitonicSorter = new LodBitonicSorter(_config.LodBitonicSorter, _numberOfInstances);

        Initialize(positions, rotations, scales);
        RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
        
        // _matricesInitializer.Initialize(positions, rotations, scales);
        // _matricesInitializer.Dispatch();
    }

    // ???
    // public void Update(List<Vector3> positions, 
    //     List<Vector3> rotations, 
    //     List<Vector3> scales)
    // {
    //     // Global data ???
    //     _cameraPosition = _config.RenderCamera.transform.position;
    //     
    //     _matricesInitializer.Initialize(positions, rotations, scales);
    //     _matricesInitializer.Dispatch();
    // }

    public void Dispose()
    {
        ShaderBuffers.Dispose();
        RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
    }

    private void Initialize(List<Vector3> positions, List<Vector3> rotations, List<Vector3> scales)
    {
        _matricesHandler.Initialize(positions, rotations, scales);
        _matricesHandler.Dispatch();
        
        //_cameraPosition = _config.RenderCamera.transform.position; // ???
        
        var hiZBuffer = new HiZBuffer(_hiZBufferConfig, _config.RenderCamera);
        _culler.Initialize(_settings, hiZBuffer);
        
        _lodBitonicSorter.ComputeAsync = _settings.ComputeAsync;
        _lodBitonicSorter.Initialize(positions, _cameraPosition);
    }

    private void OnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
    {
        if (_settings.RunCompute)
        {
            Profiler.BeginSample("CalculateVisibleInstances");
            CalculateVisibleInstances();
            Profiler.EndSample();
        }
        
        if (_settings.DrawInstances)
        {
            Profiler.BeginSample("DrawInstances");
            DrawInstances();
            Profiler.EndSample();
        }
        
        if (_settings.DrawShadows)
        {
            Profiler.BeginSample("DrawInstanceShadows");
            DrawShadows();
            Profiler.EndSample();
        }
        
        // if (debugDrawHiZ)
        // {
        //     Vector3 pos = transform.position;
        //     pos.y = debugCamera.transform.position.y;
        //     debugCamera.transform.position = pos;
        //     debugCamera.Render();
        // }
    }

    private void CalculateVisibleInstances()
    {
        // Global data
        _cameraPosition = _config.RenderCamera.transform.position;
        _bounds.center = _cameraPosition;
        
        if (_config.LogMatrices)
        {
            _config.LogMatrices = false;
            _matricesHandler.LogInstanceDrawMatrices("LogInstanceDrawMatrices");
        }
        
        Profiler.BeginSample("Resetting args buffer");
        {
            ShaderBuffers.Args.SetData(_args);
            ShaderBuffers.ShadowsArgs.SetData(_args);
            
            if (_config.LogArgumentsBuferAfterReset)
            {
                _config.LogArgumentsBuferAfterReset = false;
                // LogArgsBuffers("LogArgsBuffers - Instances After Reset", "LogArgsBuffers - Shadows After Reset");
            }
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Occlusion");
        {
            _culler.Dispatch();
            if (_config.LogArgumentsAfterOcclusion)
            {
                _config.LogArgumentsAfterOcclusion = false;
                // LogArgsBuffers("LogArgsBuffers - Instances After Occlusion", "LogArgsBuffers - Shadows After Occlusion");
            }
            
            if (_config.LogInstancesIsVisibleBuffer)
            {
                _config.LogInstancesIsVisibleBuffer = false;
                //LogInstancesIsVisibleBuffers("LogInstancesIsVisibleBuffers - Instances", "LogInstancesIsVisibleBuffers - Shadows");
            }
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("LOD Sorting");
        {
            // m_lastCamPosition = m_camPosition;
            _lodBitonicSorter.Dispatch();
        }
        Profiler.EndSample();
        
        if (_config.LogSortingData)
        {
            _config.LogSortingData = false;
            _lodBitonicSorter.LogSortingData("LogSortingData");
        }
    }

    private void DrawInstances()
    {
        Graphics.DrawMeshInstancedIndirect(
            mesh: _meshProperties.Mesh,
            submeshIndex: 0,
            material: _meshProperties.Material,
            bounds: new Bounds(Vector3.zero, Vector3.one * 1000),
            bufferWithArgs: ShaderBuffers.Args,
            argsOffset: 0, //ARGS_BYTE_SIZE_PER_DRAW_CALL,
            properties: _meshProperties.Lod2PropertyBlock,
            castShadows: ShadowCastingMode.On,
            receiveShadows: true);
        // camera: Camera.main); 
    }
    
    private void DrawShadows()
    {
        
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
        args[0] = _meshProperties.Lod0Indices;  // 0 - index count per instance, 
        args[1] = 1;                            // 1 - instance count
        args[2] = 0;                            // 2 - start index location
        args[3] = 0;                            // 3 - base vertex location
        args[4] = 0;                            // 4 - start instance location
        
        // Lod 1
        args[5] = _meshProperties.Lod1Indices;  // 0 - index count per instance, 
        args[6] = 1;                            // 1 - instance count
        args[7] = args[0] + args[2];            // 2 - start index location
        args[8] = 0;                            // 3 - base vertex location
        args[9] = 0;                            // 4 - start instance location
        
        // Lod 2
        args[10] = _meshProperties.Lod2Indices; // 0 - index count per instance, 
        args[11] = 1;                           // 1 - instance count
        args[12] = args[5] + args[7];           // 2 - start index location
        args[13] = 0;                           // 3 - base vertex location
        args[14] = 0;                           // 4 - start instance location

        return args;
    }
    
    // TODO: Implement for multiple Meshes
    // private void LogArgsBuffers(string instancePrefix = "", string shadowPrefix = "")
    // {
    //     var instancesArgs = new uint[_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE];
    //     uint[] shadowArgs = new uint[_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE];
    //     ShaderBuffers.InstancesArgsBuffer.GetData(instancesArgs);
    //     ShaderBuffers.ShadowsArgsBuffer.GetData(shadowArgs);
    //     
    //     var instancesSB = new StringBuilder();
    //     var shadowsSB = new StringBuilder();
    //     
    //     if (!string.IsNullOrEmpty(instancePrefix)) instancesSB.AppendLine(instancePrefix);
    //     if (!string.IsNullOrEmpty(shadowPrefix)) shadowsSB.AppendLine(shadowPrefix);
    //     
    //     instancesSB.AppendLine("");
    //     shadowsSB.AppendLine("");
    //     
    //     instancesSB.AppendLine("IndexCountPerInstance InstanceCount StartIndexLocation BaseVertexLocation StartInstanceLocation");
    //     shadowsSB.AppendLine("IndexCountPerInstance InstanceCount StartIndexLocation BaseVertexLocation StartInstanceLocation");
    //
    //     int counter = 0;
    //     instancesSB.AppendLine(_meshProperties.Mesh.name);
    //     shadowsSB.AppendLine(_meshProperties.Mesh.name);
    //     for (int i = 0; i < instancesArgs.Length; i++)
    //     {
    //         instancesSB.Append(instancesArgs[i] + " ");
    //         shadowsSB.Append(shadowArgs[i] + " ");
    //
    //         if ((i + 1) % 5 == 0)
    //         {
    //             instancesSB.AppendLine("");
    //             shadowsSB.AppendLine("");
    //
    //             if ((i + 1) < instancesArgs.Length
    //                 && (i + 1) % NUMBER_OF_ARGS_PER_INSTANCE_TYPE == 0)
    //             {
    //                 instancesSB.AppendLine("");
    //                 shadowsSB.AppendLine("");
    //
    //                 counter++;
    //                 var irm = _meshProperties;
    //                 Mesh m = _meshProperties.Mesh;
    //                 instancesSB.AppendLine(m.name);
    //                 shadowsSB.AppendLine(m.name);
    //             }
    //         }
    //     }
    //     
    //     Debug.Log(instancesSB.ToString());
    //     Debug.Log(shadowsSB.ToString());
    // }
}
