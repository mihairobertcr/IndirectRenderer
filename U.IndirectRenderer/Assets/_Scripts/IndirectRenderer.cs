using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class IndirectRenderer : IDisposable
{
    private readonly IndirectMesh[] _instances;
    private readonly IndirectRendererConfig _config;
    private readonly IndirectRendererSettings _settings;
    private readonly MeshProperties[] _meshProperties;
    private readonly RendererDataContext _context;

    private readonly MatricesInitializerDispatcher _matricesInitializerDispatcher;
    private readonly LodBitonicSorter _lodBitonicSorter;
    private readonly InstancesCuller _instancesCuller;
    private readonly InstancesScanner _instancesScanner;
    private readonly GroupSumsScanner _groupSumsScanner;
    private readonly InstancesDataCopier _dataCopier;

    private int _numberOfInstances = 16384;

    private Bounds _bounds;
    
    public IndirectRenderer(IndirectMesh[] instances,
        IndirectRendererConfig config, 
        IndirectRendererSettings settings)
    {
        _instances = instances;
        _config = config;
        _settings = settings;
        
        _meshProperties = CreateMeshProperties();
        _bounds.extents = Vector3.one * 10000; // ???
        
        _context = new RendererDataContext(_meshProperties, _numberOfInstances * _meshProperties.Length, _config);

        _matricesInitializerDispatcher = new MatricesInitializerDispatcher(_config.MatricesInitializer, _context);
        _lodBitonicSorter = new LodBitonicSorter(_config.LodBitonicSorter, _context);
        _instancesCuller = new InstancesCuller(_config.InstancesCuller, _context);
        _instancesScanner = new InstancesScanner(_config.InstancesScanner, _context);
        _groupSumsScanner = new GroupSumsScanner(_config.GroupSumsScanner, _context);
        _dataCopier = new InstancesDataCopier(_config.InstancesDataCopier, _context);

        Initialize();
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
    
    private void Initialize()
    {
        _matricesInitializerDispatcher.SetTransformData(_instances)
            .SubmitTransformsData()
            .Dispatch();

        _lodBitonicSorter.SetSortingData(_instances, _config.RenderCamera)
            .SetupSortingCommand()
            .EnabledAsyncComputing(true);
        
        _instancesCuller.SetSettings(_settings)
            .SetBoundsData(_instances)
            .SetDepthMap()
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
            Profiler.BeginSample("DrawMeshes");
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
            _context.Transform.LogMatrices("Matrices");
        }
        
        Profiler.BeginSample("Resetting Args Buffer");
        _context.Arguments.Reset();
        if (_config.LogArgumentsBufferAfterReset)
        {
            _config.LogArgumentsBufferAfterReset = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Reset", "Arguments Buffers - Shadows After Reset");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Occlusion");
        _instancesCuller.SubmitCameraData(_config.RenderCamera).Dispatch();
        if (_config.LogArgumentsAfterOcclusion)
        {
            _config.LogArgumentsAfterOcclusion = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Occlusion", "Arguments Buffers - Shadows After Occlusion");
        }
        
        if (_config.LogInstancesIsVisibleBuffer)
        {
            _config.LogInstancesIsVisibleBuffer = false;
            _context.Visibility.Log("Visibility Buffers - Meshes", "Visibility Buffers - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Instances");
        _instancesScanner.SubmitMeshesData().Dispatch();
        _instancesScanner.SubmitShadowsData().Dispatch();
        if (_config.LogGroupSumsBuffer)
        {
            _config.LogGroupSumsBuffer = false;
            _context.GroupSums.Log("Group Sums Buffer - Meshes", "Group Sums Buffer - Shadows");
        }
        
        if (_config.LogScannedPredicates)
        {
            _config.LogScannedPredicates = false;
            _context.ScannedPredicates.Log("Scanned Predicates - Meshes", "Scanned Predicates - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Thread Groups");
        _groupSumsScanner.SubmitMeshData().Dispatch();
        _groupSumsScanner.SubmitShadowsData().Dispatch();
        if (_config.LogScannedGroupSumsBuffer)
        {
            _config.LogScannedGroupSumsBuffer = false;
            _context.ScannedGroupSums.Log("Scanned Group Sums Buffer - Meshes", "Scanned Group Sums Buffer - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Copy Instances Data");
        _dataCopier.SubmitMeshesData().Dispatch();
        _dataCopier.SubmitShadowsData().Dispatch();

        if (_config.LogCulledMatrices)
        {
            _config.LogCulledMatrices = false;
            _context.Transform.LogCulledMatrices("Culled Matrices - Meshes", "Culled Matrices - Shadows");
        }
        
        if (_config.LogArgsBufferAfterCopy)
        {
            _config.LogArgsBufferAfterCopy = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Copy", "Arguments Buffers - Shadows After Copy");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("LOD Sorting");
        _lodBitonicSorter.Dispatch();
        Profiler.EndSample();
        
        if (_config.LogSortingData)
        {
            _config.LogSortingData = false;
            _context.Sorting.Log("Sorting Data");
        }
    }

    private void DrawInstances()
    {
        for (var i = 0; i < _meshProperties.Length; i++)
        {
            var property = _meshProperties[i];
            var rp = new RenderParams(property.Material);
            rp.worldBounds = _bounds;

            if (_settings.EnableLod)
            {
                rp.matProps = property.Lod0PropertyBlock;
                Graphics.RenderMeshIndirect(rp, property.Mesh, _context.Arguments.MeshesBuffer, 1, i * 3 + 0);
        
                rp.matProps = property.Lod1PropertyBlock;
                Graphics.RenderMeshIndirect(rp, property.Mesh, _context.Arguments.MeshesBuffer, 1, i * 3 + 1);
            }

            rp.matProps = property.Lod2PropertyBlock;
            Graphics.RenderMeshIndirect(rp, property.Mesh, _context.Arguments.MeshesBuffer, 1, i * 3 + 2);
        }
    }
    
    private void DrawShadows()
    {
        
    }

    private MeshProperties[] CreateMeshProperties()
    {
        var properties = new MeshProperties[_instances.Length];
        for (var i = 0; i < properties.Length; i++)
        {
            ref var property = ref properties[i];
            var instance = _instances[i];
            property = new MeshProperties
            {
                Mesh = new Mesh(),
                Material = instance.Material,
            
                // Lod0Vertices = (uint)_config.Lod0Mesh.vertexCount,
                // Lod1Vertices = (uint)_config.Lod1Mesh.vertexCount,
                // Lod2Vertices = (uint)_config.Lod2Mesh.vertexCount,
                //
                // Lod0Indices = _config.Lod0Mesh.GetIndexCount(0),
                // Lod1Indices = _config.Lod1Mesh.GetIndexCount(0),
                // Lod2Indices = _config.Lod2Mesh.GetIndexCount(0),
                
                Lod0PropertyBlock = new MaterialPropertyBlock(),
                Lod1PropertyBlock = new MaterialPropertyBlock(),
                Lod2PropertyBlock = new MaterialPropertyBlock(),
            
                ShadowLod0PropertyBlock = new MaterialPropertyBlock(),
                ShadowLod1PropertyBlock = new MaterialPropertyBlock(),
                ShadowLod2PropertyBlock = new MaterialPropertyBlock()
            };
        
            property.Mesh.name = instance.Prefab.name;
            var combinedMeshes = new CombineInstance[]
            {
                new() { mesh = instance.Lod0Mesh },
                new() { mesh = instance.Lod1Mesh },
                new() { mesh = instance.Lod2Mesh }
            };
        
            property.Mesh.CombineMeshes(
                combine: combinedMeshes,
                mergeSubMeshes: false,
                useMatrices: false,
                hasLightmapData: false);
        }

        return properties;
    }
}
