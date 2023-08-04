using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class IndirectRenderer : IDisposable
{
    //TODO: Extract to wrappers base Class
    public const int NUMBER_OF_ARGS_PER_INSTANCE_TYPE = NUMBER_OF_DRAW_CALLS * NUMBER_OF_ARGS_PER_DRAW; // 3draws * 5args = 15args

    private const int NUMBER_OF_DRAW_CALLS = 3;                                                         // (LOD00 + LOD01 + LOD02)
    private const int NUMBER_OF_ARGS_PER_DRAW = 5;                                                      // (indexCount, instanceCount, startIndex, baseVertex, startInstance)
    private const int ARGS_BYTE_SIZE_PER_DRAW_CALL = NUMBER_OF_ARGS_PER_DRAW * sizeof(uint);            // 5args * 4bytes = 20 bytes

    private readonly IndirectRendererConfig _config;
    private readonly IndirectRendererSettings _settings;
    private readonly HierarchicalDepthBufferConfig _hierarchicalDepthBufferConfig;
    private readonly MeshProperties _meshProperties;
    private readonly uint[] _args;
    
    private readonly MatricesInitializer _matricesInitializer;
    private readonly LodBitonicSorter _lodBitonicSorter;
    private readonly InstancesCuller _instancesCuller;
    private readonly InstancesScanner _instancesScanner;
    private readonly GroupSumsScanner _groupSumsScanner;
    private readonly InstancesDataCopier _dataCopier;

    private int _numberOfInstances = 16384;
    private int _numberOfInstanceTypes = 1;
    
    private List<Vector3> _positions;
    private List<Vector3> _scales;
    private List<Vector3> _rotations;
    
    private Vector3 _cameraPosition = Vector3.zero;
    private Bounds _bounds;

    private RendererDataContext _context;

    public IndirectRenderer(IndirectRendererConfig config, 
        IndirectRendererSettings settings, 
        HierarchicalDepthBufferConfig hierarchicalDepthBufferConfig,
        List<Vector3> positions, 
        List<Vector3> rotations, 
        List<Vector3> scales)
    {
        _config = config;
        _settings = settings;
        _hierarchicalDepthBufferConfig = hierarchicalDepthBufferConfig;

        _context = new RendererDataContext();
        
        _meshProperties = CreateMeshProperties();
        _args = InitializeArgumentsBuffer();
        _bounds.extents = Vector3.one * 10000; // ???
        
        ShaderKernels.Initialize(_config);

        // Note: Considering we have multiple types of meshes
        // I don't know how this is working right now but
        // it should preserve the sorting functionality
        var count = NUMBER_OF_ARGS_PER_INSTANCE_TYPE; // * _numberOfInstanceTypes
        _context.Args = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.IndirectArguments);
        _context.ShadowsArgs = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.IndirectArguments);
        _context.Args.SetData(_args);
        _context.ShadowsArgs.SetData(_args);

        _matricesInitializer = new MatricesInitializer(_config.MatricesInitializer, _numberOfInstances, _context); //, _meshProperties);
        _lodBitonicSorter = new LodBitonicSorter(_config.LodBitonicSorter, _numberOfInstances, _context);
        _instancesCuller = new InstancesCuller(_config.InstancesCuller, _numberOfInstances, _context, _config.RenderCamera);
        _instancesScanner = new InstancesScanner(_config.InstancesScanner, _numberOfInstances, _context);
        _groupSumsScanner = new GroupSumsScanner(_config.GroupSumsScanner, _numberOfInstances, _context);
        _dataCopier = new InstancesDataCopier(_config.InstancesDataCopier, _numberOfInstances, _context, _numberOfInstanceTypes);
        

        Initialize(positions, rotations, scales);
        RenderPipelineManager.beginFrameRendering += BeginFrameRendering;
        
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
        _context.Dispose();
        RenderPipelineManager.beginFrameRendering -= BeginFrameRendering;
        
        // args0Buffer.Release();
        // args1Buffer.Release();
        // args2Buffer.Release();
    }

    public void DrawGizmos()
    {
        if (_config.DebugBounds)
        {
            _instancesCuller.DrawGizmos();
        }
    }
    
    private void Initialize(List<Vector3> positions, List<Vector3> rotations, List<Vector3> scales)
    {
        _matricesInitializer.Initialize(positions, rotations, scales);
        _matricesInitializer.Dispatch();
        
        //_cameraPosition = _config.RenderCamera.transform.position; // ???

        var cameraPosition = _config.RenderCamera.transform.position;
        _lodBitonicSorter.Initialize(positions, cameraPosition);
        _lodBitonicSorter.ComputeAsync = _settings.ComputeAsync;
        
        // var hiZBuffer = new HiZBuffer(_hierarchicalDepthBufferConfig, _config.RenderCamera);
        _instancesCuller.Initialize(positions, scales, _settings, _hierarchicalDepthBufferConfig);
        
        _instancesScanner.Initialize();
        _groupSumsScanner.Initialize();
        _dataCopier.Initialize(_meshProperties, _config);
    }

    public void BeginFrameRendering(ScriptableRenderContext context, Camera[] camera)
    // public void Update()
    {
        // Debug.Log("OnBeginCameraRendering");
        // if (_config.RenderCamera != camera) return;
        
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
        
        // if (_settings.DrawShadows)
        // {
        //     Profiler.BeginSample("DrawInstanceShadows");
        //     DrawShadows();
        //     Profiler.EndSample();
        // }
        
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
        // _cameraPosition = _config.RenderCamera.transform.position;
        _bounds.center = _config.RenderCamera.transform.position;
        
        if (_config.LogMatrices)
        {
            _config.LogMatrices = false;
            _matricesInitializer.LogInstanceDrawMatrices("LogInstanceDrawMatrices");
        }
        
        Profiler.BeginSample("Resetting args buffer");
        _context.Args.SetData(_args);
        _context.ShadowsArgs.SetData(_args);
        
        if (_config.LogArgumentsBufferAfterReset)
        {
            _config.LogArgumentsBufferAfterReset = false;
            LogArgsBuffers("LogArgsBuffers - Instances After Reset", "LogArgsBuffers - Shadows After Reset");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Occlusion");
        _instancesCuller.Dispatch();
        if (_config.LogArgumentsAfterOcclusion)
        {
            _config.LogArgumentsAfterOcclusion = false;
            LogArgsBuffers("LogArgsBuffers - Instances After Occlusion", "LogArgsBuffers - Shadows After Occlusion");
        }
        
        if (_config.LogInstancesIsVisibleBuffer)
        {
            _config.LogInstancesIsVisibleBuffer = false;
            LogInstancesIsVisibleBuffers("LogInstancesIsVisibleBuffers - Instances", "LogInstancesIsVisibleBuffers - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Instances");
        _instancesScanner.Dispatch();
        if (_config.LogGroupSumsBuffer)
        {
            _config.LogGroupSumsBuffer = false;
            LogGroupSumsBuffer("LogGroupSumsBuffer - Instances", "LogGroupSumsBuffer - Shadows");
        }
        
        if (_config.LogScannedPredicates)
        {
            _config.LogScannedPredicates = false;
            LogScannedPredicates("LogScannedPredicates - Instances", "LogScannedPredicates - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Thread Groups");
        _groupSumsScanner.Dispatch();
        if (_config.LogScannedGroupSumsBuffer)
        {
            _config.LogScannedGroupSumsBuffer = false;
            LogScannedGroupSumBuffer("LogScannedGroupSumBuffer - Instances", "LogScannedGroupSumBuffer - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Copy Instance Data");
        _dataCopier.Dispatch();
        if (_config.LogCulledMatrices)
        {
            _config.LogCulledMatrices = false;
            LogCulledInstancesDrawMatrices("LogCulledMatrices - Instances", "LogCulledMatrices - Shadows");
        }
        
        if (_config.LogArgsBufferAfterCopy)
        {
            _config.LogArgsBufferAfterCopy = false;
            LogArgsBuffers("LogArgsBuffers - Instances After Copy", "LogArgsBuffers - Shadows After Copy");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("LOD Sorting");
        // m_lastCamPosition = m_camPosition;
        _lodBitonicSorter.Dispatch();
        Profiler.EndSample();
        
        if (_config.LogSortingData)
        {
            _config.LogSortingData = false;
            _lodBitonicSorter.LogSortingData("LogSortingData");
        }
    }

    private void DrawInstances()
    {
        if (_settings.EnableLod)
        {
            Graphics.DrawMeshInstancedIndirect(
                mesh: _config.Lod0Mesh, //_meshProperties.Mesh,
                submeshIndex: 0,
                material: _meshProperties.Material,
                bounds: _bounds,
                bufferWithArgs: _context.LodArgs0, //RendererDataContext.Args,
                argsOffset: 0,// ARGS_BYTE_SIZE_PER_DRAW_CALL * 0,
                properties: _meshProperties.Lod0PropertyBlock,
                castShadows: ShadowCastingMode.On);
                //camera: _config.RenderCamera);

            Graphics.DrawMeshInstancedIndirect(
                mesh: _config.Lod1Mesh, //_meshProperties.Mesh,
                submeshIndex: 0,
                material: _meshProperties.Material,
                bounds: _bounds,
                bufferWithArgs: _context.LodArgs1, //RendererDataContext.Args,
                argsOffset: 0, //ARGS_BYTE_SIZE_PER_DRAW_CALL * 1,
                properties: _meshProperties.Lod1PropertyBlock,
                castShadows: ShadowCastingMode.On);
                //camera: _config.RenderCamera);
        }

        Graphics.DrawMeshInstancedIndirect(
            mesh: _config.Lod2Mesh, //_meshProperties.Mesh,
            submeshIndex: 0,
            material: _meshProperties.Material,
            bounds: _bounds,
            bufferWithArgs: _context.LodArgs2, //RendererDataContext.Args,
            argsOffset: 0, //ARGS_BYTE_SIZE_PER_DRAW_CALL * 2,
            properties: _meshProperties.Lod2PropertyBlock,
            castShadows: ShadowCastingMode.On);
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
        
        // properties.Mesh = new Mesh();
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
        args[1] = 0;                            // 1 - instance count
        args[2] = 0;                            // 2 - start index location
        args[3] = 0;                            // 3 - base vertex location
        args[4] = 0;                            // 4 - start instance location
        
        // Lod 1
        args[5] = _meshProperties.Lod1Indices;  // 0 - index count per instance, 
        args[6] = 0;                            // 1 - instance count
        args[7] = args[0] + args[2];            // 2 - start index location
        args[8] = 0;                            // 3 - base vertex location
        args[9] = 0;                            // 4 - start instance location
        
        // Lod 2
        args[10] = _meshProperties.Lod2Indices; // 0 - index count per instance, 
        args[11] = 0;                           // 1 - instance count
        args[12] = args[5] + args[7];           // 2 - start index location
        args[13] = 0;                           // 3 - base vertex location
        args[14] = 0;                           // 4 - start instance location

        return args;
    }
    
    // TODO: Implement for multiple Meshes
    private void LogArgsBuffers(string instancePrefix = "", string shadowPrefix = "")
    {
        var args = new uint[_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE];
        var shadowArgs = new uint[_numberOfInstanceTypes * NUMBER_OF_ARGS_PER_INSTANCE_TYPE];
        _context.Args.GetData(args);
        _context.ShadowsArgs.GetData(shadowArgs);
        
        var instancesSB = new StringBuilder();
        var shadowsSB = new StringBuilder();
        
        if (!string.IsNullOrEmpty(instancePrefix)) instancesSB.AppendLine(instancePrefix);
        if (!string.IsNullOrEmpty(shadowPrefix)) shadowsSB.AppendLine(shadowPrefix);
        
        instancesSB.AppendLine("");
        shadowsSB.AppendLine("");
        
        instancesSB.AppendLine("IndexCountPerInstance InstanceCount StartIndexLocation BaseVertexLocation StartInstanceLocation");
        shadowsSB.AppendLine("IndexCountPerInstance InstanceCount StartIndexLocation BaseVertexLocation StartInstanceLocation");
    
        // var counter = 0;
        instancesSB.AppendLine(_meshProperties.Mesh.name);
        shadowsSB.AppendLine(_meshProperties.Mesh.name);
        for (var i = 0; i < args.Length; i++)
        {
            instancesSB.Append(args[i] + " ");
            shadowsSB.Append(shadowArgs[i] + " ");

            if ((i + 1) % 5 != 0) continue;
            instancesSB.AppendLine("");
            shadowsSB.AppendLine("");

            if ((i + 1) >= args.Length || (i + 1) % NUMBER_OF_ARGS_PER_INSTANCE_TYPE != 0) continue;
            instancesSB.AppendLine("");
            shadowsSB.AppendLine("");
    
            // counter++;
            var mesh = _meshProperties.Mesh;
            instancesSB.AppendLine(mesh.name);
            shadowsSB.AppendLine(mesh.name);
        }
        
        Debug.Log(instancesSB.ToString());
        Debug.Log(shadowsSB.ToString());
    }
    
    private void LogInstancesIsVisibleBuffers(string instancePrefix = "", string shadowPrefix = "")
    {
        var instancesIsVisible = new uint[_numberOfInstances];
        var shadowsIsVisible = new uint[_numberOfInstances];
        _context.IsVisible.GetData(instancesIsVisible);
        _context.IsShadowVisible.GetData(shadowsIsVisible);
        
        var instancesSB = new StringBuilder();
        var shadowsSB = new StringBuilder();
        
        if (!string.IsNullOrEmpty(instancePrefix)) instancesSB.AppendLine(instancePrefix);
        if (!string.IsNullOrEmpty(shadowPrefix)) shadowsSB.AppendLine(shadowPrefix);
        
        for (var i = 0; i < instancesIsVisible.Length; i++)
        {
            instancesSB.AppendLine(i + ": " + instancesIsVisible[i]);
            shadowsSB.AppendLine(i + ": " + shadowsIsVisible[i]);
        }
        
        Debug.Log(instancesSB.ToString());
        Debug.Log(shadowsSB.ToString());
    }
    
    private void LogScannedPredicates(string instancePrefix = "", string shadowPrefix = "")
    {
        var instancesScannedData = new uint[_numberOfInstances];
        var shadowsScannedData = new uint[_numberOfInstances];
        _context.ScannedPredicates.GetData(instancesScannedData);
        _context.ShadowsScannedPredicates.GetData(shadowsScannedData);
        
        var instancesSB = new StringBuilder();
        var shadowsSB = new StringBuilder();
        
        if (!string.IsNullOrEmpty(instancePrefix)) instancesSB.AppendLine(instancePrefix);
        if (!string.IsNullOrEmpty(shadowPrefix)) shadowsSB.AppendLine(shadowPrefix);
        
        for (var i = 0; i < instancesScannedData.Length; i++)
        {
            instancesSB.AppendLine(i + ": " + instancesScannedData[i]);
            shadowsSB.AppendLine(i + ": " + shadowsScannedData[i]);
        }

        Debug.Log(instancesSB.ToString());
        Debug.Log(shadowsSB.ToString());
    }
    
    private void LogGroupSumsBuffer(string instancePrefix = "", string shadowPrefix = "")
    {
        var instancesScannedData = new uint[_numberOfInstances];
        var shadowsScannedData = new uint[_numberOfInstances];
        _context.GroupSumsBuffer.GetData(instancesScannedData);
        _context.ShadowsScannedGroupSums.GetData(shadowsScannedData); // Which group sums ???
        
        var instancesSB = new StringBuilder();
        var shadowsSB = new StringBuilder();
        
        if (!string.IsNullOrEmpty(instancePrefix)) instancesSB.AppendLine(instancePrefix);
        if (!string.IsNullOrEmpty(shadowPrefix)) shadowsSB.AppendLine(shadowPrefix); 
        
        for (var i = 0; i < instancesScannedData.Length; i++)
        {
            instancesSB.AppendLine(i + ": " + instancesScannedData[i]);
            shadowsSB.AppendLine(i + ": " + shadowsScannedData[i]);
        }

        Debug.Log(instancesSB.ToString());
        Debug.Log(shadowsSB.ToString());
    }
    
    private void LogScannedGroupSumBuffer(string instancePrefix = "", string shadowPrefix = "")
    {
        var instancesScannedData = new uint[_numberOfInstances];
        var shadowsScannedData = new uint[_numberOfInstances];
        _context.ScannedPredicates.GetData(instancesScannedData);
        _context.ShadowsScannedPredicates.GetData(shadowsScannedData);
        
        var instancesSB = new StringBuilder();
        var shadowsSB = new StringBuilder();
        
        if (!string.IsNullOrEmpty(instancePrefix)) instancesSB.AppendLine(instancePrefix);
        if (!string.IsNullOrEmpty(shadowPrefix)) shadowsSB.AppendLine(shadowPrefix);
        
        for (var i = 0; i < instancesScannedData.Length; i++)
        {
            instancesSB.AppendLine(i + ": " + instancesScannedData[i]);
            shadowsSB.AppendLine(i + ": " + shadowsScannedData[i]);
        }

        Debug.Log(instancesSB.ToString());
        Debug.Log(shadowsSB.ToString());
    }
    
    private void LogCulledInstancesDrawMatrices(string instancePrefix = "", string shadowPrefix = "")
    {
        var instancesMatrix1 = new Indirect2x2Matrix[_numberOfInstances];
        var instancesMatrix2 = new Indirect2x2Matrix[_numberOfInstances];
        var instancesMatrix3 = new Indirect2x2Matrix[_numberOfInstances];
        _context.CulledMatrixRows01.GetData(instancesMatrix1);
        _context.CulledMatrixRows23.GetData(instancesMatrix2);
        _context.CulledMatrixRows45.GetData(instancesMatrix3);
        
        var shadowsMatrix1 = new Indirect2x2Matrix[_numberOfInstances];
        var shadowsMatrix2 = new Indirect2x2Matrix[_numberOfInstances];
        var shadowsMatrix3 = new Indirect2x2Matrix[_numberOfInstances];
        _context.ShadowsCulledMatrixRows01.GetData(shadowsMatrix1);
        _context.ShadowsCulledMatrixRows23.GetData(shadowsMatrix2);
        _context.ShadowsCulledMatrixRows45.GetData(shadowsMatrix3);
        
        var instancesSB = new StringBuilder();
        var shadowsSB = new StringBuilder();
        
        if (!string.IsNullOrEmpty(instancePrefix)){ instancesSB.AppendLine(instancePrefix); }
        if (!string.IsNullOrEmpty(shadowPrefix))  { shadowsSB.AppendLine(shadowPrefix); }
        
        for (int i = 0; i < instancesMatrix1.Length; i++)
        {
            instancesSB.AppendLine(
                i + "\n" 
                + instancesMatrix1[i].FirstRow + "\n"
                + instancesMatrix1[i].SecondRow + "\n"
                + instancesMatrix2[i].FirstRow + "\n"
                + "\n\n"
                + instancesMatrix2[i].SecondRow + "\n"
                + instancesMatrix3[i].FirstRow + "\n"
                + instancesMatrix3[i].SecondRow + "\n"
                + "\n"
            );
            
            shadowsSB.AppendLine(
                i + "\n" 
                + shadowsMatrix1[i].FirstRow + "\n"
                + shadowsMatrix1[i].SecondRow + "\n"
                + shadowsMatrix2[i].FirstRow + "\n"
                + "\n\n"
                + shadowsMatrix2[i].SecondRow + "\n"
                + shadowsMatrix3[i].FirstRow + "\n"
                + shadowsMatrix3[i].SecondRow + "\n"
                + "\n"
            );
        }

        Debug.Log(instancesSB.ToString());
        Debug.Log(shadowsSB.ToString());
    }
}
