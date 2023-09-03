using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class IndirectRenderer : IDisposable
{
    private readonly IndirectRendererConfig _config;
    private readonly IndirectRendererSettings _settings;
    private readonly HierarchicalDepthMap _hierarchicalDepthMap;
    private readonly MeshProperties _meshProperties;
    
    private readonly MatricesInitializer _matricesInitializer;
    private readonly LodBitonicSorter _lodBitonicSorter;
    private readonly InstancesCuller _instancesCuller;
    private readonly InstancesScanner _instancesScanner;
    private readonly GroupSumsScanner _groupSumsScanner;
    private readonly InstancesDataCopier _dataCopier;

    private int _numberOfInstances = 16384;
    
    private List<Vector3> _positions;
    private List<Vector3> _scales;
    private List<Vector3> _rotations;
    
    private Vector3 _cameraPosition = Vector3.zero;
    private Bounds _bounds;

    private RendererDataContext _context;

    public IndirectRenderer(IndirectRendererConfig config, 
        IndirectRendererSettings settings, 
        HierarchicalDepthMap hierarchicalDepthMap,
        List<Vector3> positions, 
        List<Vector3> rotations, 
        List<Vector3> scales)
    {
        _config = config;
        _settings = settings;
        _hierarchicalDepthMap = hierarchicalDepthMap;
        
        _meshProperties = CreateMeshProperties();
        _bounds.extents = Vector3.one * 10000; // ???
        
        _context = new RendererDataContext(_meshProperties, _numberOfInstances, _config);

        _matricesInitializer = new MatricesInitializer(_config.MatricesInitializer, _context);
        _lodBitonicSorter = new LodBitonicSorter(_config.LodBitonicSorter, _context);
        _instancesCuller = new InstancesCuller(_config.InstancesCuller, _context);
        _instancesScanner = new InstancesScanner(_config.InstancesScanner, _context);
        _groupSumsScanner = new GroupSumsScanner(_config.GroupSumsScanner, _context);
        _dataCopier = new InstancesDataCopier(_config.InstancesDataCopier, _context);
        

        Initialize(positions.ToArray(), rotations.ToArray(), scales.ToArray());
        RenderPipelineManager.beginFrameRendering += BeginFrameRendering;
    }

    public void Dispose()
    {
        _context.Dispose();
        RenderPipelineManager.beginFrameRendering -= BeginFrameRendering;
    }

    public void DrawGizmos()
    {
        if (_config.DebugBounds)
        {
            _instancesCuller.DrawGizmos();
        }
    }
    
    private void Initialize(Vector3[] positions, Vector3[] rotations, Vector3[] scales)
    {
        _matricesInitializer.SetTransformData(positions, rotations, scales)
            .SubmitTransformsData()
            .Dispatch();

        _lodBitonicSorter.SetSortingData(positions, _config.RenderCamera)
            .SetupSortingCommand()
            .EnabledAsyncComputing(true);
        
        _instancesCuller.SetSettings(_settings)
            .SetBoundsData(positions, scales)
            .SetDepthMap(_hierarchicalDepthMap)
            .SubmitCullingData();

        _groupSumsScanner.SubmitGroupCount();
        _dataCopier.SubmitCopingBuffers();
        _dataCopier.BindMaterialProperties(_meshProperties);
    }

    public void BeginFrameRendering(ScriptableRenderContext context, Camera[] camera)
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
        _bounds.center = _config.RenderCamera.transform.position;
        
        if (_config.LogMatrices)
        {
            _config.LogMatrices = false;
            _context.Transform.LogMatrices("LogInstanceDrawMatrices");
        }
        
        Profiler.BeginSample("Resetting args buffer");
        _context.Arguments.Reset();
        
        if (_config.LogArgumentsBufferAfterReset)
        {
            _config.LogArgumentsBufferAfterReset = false;
            _context.Arguments.Log("LogArgsBuffers - Instances After Reset", "LogArgsBuffers - Shadows After Reset");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Occlusion");
        _instancesCuller.SubmitCameraData(_config.RenderCamera).Dispatch();
        if (_config.LogArgumentsAfterOcclusion)
        {
            _config.LogArgumentsAfterOcclusion = false;
            _context.Arguments.Log("LogArgsBuffers - Instances After Occlusion", "LogArgsBuffers - Shadows After Occlusion");
        }
        
        if (_config.LogInstancesIsVisibleBuffer)
        {
            _config.LogInstancesIsVisibleBuffer = false;
            _context.Visibility.Log("LogInstancesIsVisibleBuffers - Instances", "LogInstancesIsVisibleBuffers - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Instances");
        _instancesScanner.SubmitMeshesData().Dispatch();
        _instancesScanner.SubmitShadowsData().Dispatch();
        if (_config.LogGroupSumsBuffer)
        {
            _config.LogGroupSumsBuffer = false;
            _context.GroupSums.Log("LogGroupSumsBuffer - Instances", "LogGroupSumsBuffer - Shadows");
        }
        
        if (_config.LogScannedPredicates)
        {
            _config.LogScannedPredicates = false;
            _context.ScannedPredicates.Log("LogScannedPredicates - Instances", "LogScannedPredicates - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Thread Groups");
        _groupSumsScanner.SubmitMeshData().Dispatch();
        _groupSumsScanner.SubmitShadowsData().Dispatch();
        if (_config.LogScannedGroupSumsBuffer)
        {
            _config.LogScannedGroupSumsBuffer = false;
            _context.ScannedGroupSums.Log("LogScannedGroupSumBuffer - Instances", "LogScannedGroupSumBuffer - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Copy Instance Data");
        _dataCopier.SubmitMeshesData().Dispatch();
        _dataCopier.SubmitShadowsData().Dispatch();
        // _dataCopier.SubmitArgumentsData().DispatchArgumentsSplitter();

        if (_config.LogCulledMatrices)
        {
            _config.LogCulledMatrices = false;
            _context.Transform.LogCulledMatrices("LogCulledMatrices - Instances", "LogCulledMatrices - Shadows");
        }
        
        if (_config.LogArgsBufferAfterCopy)
        {
            _config.LogArgsBufferAfterCopy = false;
            _context.Arguments.Log("LogArgsBuffers - Instances After Copy", "LogArgsBuffers - Shadows After Copy");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("LOD Sorting");
        _lodBitonicSorter.Dispatch();
        Profiler.EndSample();
        
        if (_config.LogSortingData)
        {
            _config.LogSortingData = false;
            _context.Sorting.Log("LogSortingData");
        }
    }

    private void DrawInstances()
    {
        var rp = new RenderParams(_meshProperties.Material);
        rp.worldBounds = _bounds;
        rp.shadowCastingMode = ShadowCastingMode.On;

        // var args = new uint[15];
        // _context.Arguments.MeshesBuffer.GetData(args);
        
        if (_settings.EnableLod)
        {
            // rp.matProps = _meshProperties.Lod0PropertyBlock;
            // Graphics.RenderMeshIndirect(rp, _config.Lod0Mesh, _context.Arguments.LodArgs0, 1, 0);
            //
            // rp.matProps = _meshProperties.Lod1PropertyBlock;
            // Graphics.RenderMeshIndirect(rp, _config.Lod1Mesh, _context.Arguments.LodArgs1, 1, 0);
            
            rp.matProps = _meshProperties.Lod0PropertyBlock;
            Graphics.RenderMeshIndirect(rp, _meshProperties.Mesh, _context.Arguments.MeshesBuffer, 1, 0);
            
            rp.matProps = _meshProperties.Lod1PropertyBlock;
            Graphics.RenderMeshIndirect(rp, _meshProperties.Mesh, _context.Arguments.MeshesBuffer, 1, 1);
        }

        // rp.matProps = _meshProperties.Lod2PropertyBlock;
        // Graphics.RenderMeshIndirect(rp, _config.Lod2Mesh, _context.Arguments.LodArgs2, 1, 0);
        
        rp.matProps = _meshProperties.Lod2PropertyBlock;
        Graphics.RenderMeshIndirect(rp, _meshProperties.Mesh, _context.Arguments.MeshesBuffer, 1, 2);
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
        
        properties.Mesh.name = "Mesh";
        var meshes = new CombineInstance[]
        {
            new() { mesh = _config.Lod0Mesh },
            new() { mesh = _config.Lod1Mesh },
            new() { mesh = _config.Lod2Mesh }
        };
        
        properties.Mesh.CombineMeshes(
            combine: meshes,
            mergeSubMeshes: false,
            useMatrices: false,
            hasLightmapData: false);

        return properties;
    }

 
}
