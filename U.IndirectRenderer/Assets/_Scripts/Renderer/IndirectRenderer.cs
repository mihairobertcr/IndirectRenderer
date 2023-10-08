using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class IndirectRenderer : IDisposable
{
    private readonly InstanceProperties[] _instances;
    private readonly IndirectRendererConfig _config;
    private readonly IndirectRendererSettings _settings;
    private readonly RendererDataContext _context;

    private readonly MatricesInitializerDispatcher _matricesInitializerDispatcher;
    private readonly LodBitonicSorter _lodBitonicSorter;
    private readonly InstancesCuller _instancesCuller;
    private readonly InstancesScanner _instancesScanner;
    private readonly GroupSumsScanner _groupSumsScanner;
    private readonly InstancesDataCopier _dataCopier;

    private Bounds _bounds;
    
    public IndirectRenderer(InstanceProperties[] instances,
        IndirectRendererConfig config, 
        IndirectRendererSettings settings)
    {
        _instances = instances;
        _config = config;
        _settings = settings;
        
        InitializeMeshProperties();
        _bounds.extents = Vector3.one * 10000; // ???
        
        _context = new RendererDataContext(config, _instances);

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
        if (_settings.DrawBounds)
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
            .SetLodsData(_instances)
            .SetDepthMap()
            .SubmitCullingData();

        _groupSumsScanner.SubmitGroupCount();
        _dataCopier.SubmitCopingBuffers();
        _dataCopier.BindMaterialProperties(_instances);
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
        
        if (_settings.LogMatrices)
        {
            _settings.LogMatrices = false;
            _context.Transforms.LogMatrices("Matrices");
        }
        
        Profiler.BeginSample("Resetting Args Buffer");
        _context.Arguments.Reset();
        if (_settings.LogArgumentsAfterReset)
        {
            _settings.LogArgumentsAfterReset = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Reset", "Arguments Buffers - Shadows After Reset");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("LOD Sorting");
        _lodBitonicSorter.Dispatch();
        if (_settings.LogSortingData)
        {
            _settings.LogSortingData = false;
            _context.Sorting.Log("Sorting Data");
        }
        Profiler.EndSample();

        Profiler.BeginSample("Occlusion");
        _instancesCuller.SubmitCameraData(_config.RenderCamera)
            .SubmitLodsData()
            .Dispatch();
        
        if (_settings.LogArgumentsAfterOcclusion)
        {
            _settings.LogArgumentsAfterOcclusion = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Occlusion", "Arguments Buffers - Shadows After Occlusion");
        }
        
        if (_settings.LogVisibilityBuffer)
        {
            _settings.LogVisibilityBuffer = false;
            _context.Visibility.Log("Visibility Buffers - Meshes", "Visibility Buffers - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Instances");
        _instancesScanner.SubmitMeshesData().Dispatch();
        _instancesScanner.SubmitShadowsData().Dispatch();
        if (_settings.LogGroupSums)
        {
            _settings.LogGroupSums = false;
            _context.GroupSums.Log("Group Sums Buffer - Meshes", "Group Sums Buffer - Shadows");
        }
        
        if (_settings.LogScannedPredicates)
        {
            _settings.LogScannedPredicates = false;
            _context.ScannedPredicates.Log("Scanned Predicates - Meshes", "Scanned Predicates - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Thread Groups");
        _groupSumsScanner.SubmitMeshData().Dispatch();
        _groupSumsScanner.SubmitShadowsData().Dispatch();
        if (_settings.LogScannedGroupSums)
        {
            _settings.LogScannedGroupSums = false;
            _context.ScannedGroupSums.Log("Scanned Group Sums Buffer - Meshes", "Scanned Group Sums Buffer - Shadows");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Copy Instances Data");
        _dataCopier.SubmitMeshesData().Dispatch();
        _dataCopier.SubmitShadowsData().Dispatch();

        if (_settings.LogCulledMatrices)
        {
            _settings.LogCulledMatrices = false;
            _context.Transforms.LogCulledMatrices("Culled Matrices - Meshes", "Culled Matrices - Shadows");
        }
        
        if (_settings.LogArgumentsAfterCopy)
        {
            _settings.LogArgumentsAfterCopy = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Copy", "Arguments Buffers - Shadows After Copy");
        }
        Profiler.EndSample();
        

    }

    private void DrawInstances()
    {
        for (var i = 0; i < _instances.Length; i++)
        {
            var property = _instances[i];
            var rp = new RenderParams(property.Material);
            rp.worldBounds = _bounds;

            // if (_settings.EnableLod)
            // {
            //     rp.matProps = property.Lod0PropertyBlock;
            //     Graphics.RenderMeshIndirect(rp, property.Lod0Mesh, _context.Arguments.MeshesBuffer, 1, i * 3 + 0);
            //
            //     rp.matProps = property.Lod1PropertyBlock;
            //     Graphics.RenderMeshIndirect(rp, property.Lod1Mesh, _context.Arguments.MeshesBuffer, 1, i * 3 + 1);
            // }
            //
            // rp.matProps = property.Lod2PropertyBlock;
            // Graphics.RenderMeshIndirect(rp, property.Lod2Mesh, _context.Arguments.MeshesBuffer, 1, i * 3 + 2);

            for (var k = 0; k < property.Lods.Count; k++)
            {
                var lod = property.Lods[k];
                rp.matProps = lod.MeshPropertyBlock;
                var startCommand = i * property.Lods.Count + k;
                Graphics.RenderMeshIndirect(rp, lod.Mesh, _context.Arguments.MeshesBuffer, 1, startCommand);
            }
        }
    }
    
    private void DrawShadows()
    {
        
    }

    private void InitializeMeshProperties()
    {
        foreach (var instance in _instances)
        {
            instance.Initialize();
        }
    }
}
