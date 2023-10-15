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

    private readonly MatricesInitializingDispatcher _matricesInitializingDispatcher;
    private readonly LodsSortingDispatcher _lodsSortingDispatcher;
    private readonly CullingDispatcher _cullingDispatcher;
    private readonly PredicatesScanningDispatcher _predicatesScanningDispatcher;
    private readonly GroupSumsScanningDispatcher _groupSumsScanningDispatcher;
    private readonly DataCopyingDispatcher _dataCopyingDispatcher;

    private Bounds _worldBounds;
    
    public IndirectRenderer(InstanceProperties[] instances,
        IndirectRendererConfig config, 
        IndirectRendererSettings settings)
    {
        _instances = instances;
        _config = config;
        _settings = settings;
        
        InitializeMeshProperties();
        _worldBounds.extents = Vector3.one * 10000; // ???
        
        _context = new RendererDataContext(config, _instances);

        _matricesInitializingDispatcher = new MatricesInitializingDispatcher(_config.MatricesInitializer, _context);
        _lodsSortingDispatcher = new LodsSortingDispatcher(_config.LodBitonicSorter, _context);
        _cullingDispatcher = new CullingDispatcher(_config.InstancesCuller, _context);
        _predicatesScanningDispatcher = new PredicatesScanningDispatcher(_config.InstancesScanner, _context);
        _groupSumsScanningDispatcher = new GroupSumsScanningDispatcher(_config.GroupSumsScanner, _context);
        _dataCopyingDispatcher = new DataCopyingDispatcher(_config.InstancesDataCopier, _context);

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
            _cullingDispatcher.DrawGizmos();
        }
    }
    
    private void Initialize()
    {
        _matricesInitializingDispatcher.SetTransformData(_instances)
            .SubmitTransformsData()
            .Dispatch();

        _lodsSortingDispatcher.SetSortingData(_instances, _config.RenderCamera)
            .SetupSortingCommand()
            .EnabledAsyncComputing(true);
        
        _cullingDispatcher.SetSettings(_settings)
            .SetBoundsData(_instances)
            .SetLodsData(_instances)
            .SetDepthMap()
            .SubmitCullingData()
            .SubmitLodsData();

        _groupSumsScanningDispatcher.SubmitGroupCount();
        _dataCopyingDispatcher.SubmitCopingBuffers();
        _dataCopyingDispatcher.BindMaterialProperties(_instances);
    }

    private void BeginFrameRendering(ScriptableRenderContext context, Camera[] camera)
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
    }

    private void CalculateVisibleInstances()
    {
        _worldBounds.center = _config.RenderCamera.transform.position;
        
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
            _context.Arguments.Log("Arguments Buffers - Meshes After Reset");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("LOD Sorting");
        _lodsSortingDispatcher.Dispatch();
        if (_settings.LogSortingData)
        {
            _settings.LogSortingData = false;
            _context.Sorting.Log("Sorting Data");
        }
        Profiler.EndSample();

        Profiler.BeginSample("Occlusion");
        _cullingDispatcher.SubmitCameraData(_config.RenderCamera)
            .Dispatch();
        
        if (_settings.LogArgumentsAfterOcclusion)
        {
            _settings.LogArgumentsAfterOcclusion = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Occlusion");
        }
        
        if (_settings.LogVisibilityBuffer)
        {
            _settings.LogVisibilityBuffer = false;
            _context.LogVisibility("Visibility Buffers - Meshes");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Instances");
        _predicatesScanningDispatcher.SubmitMeshesData().Dispatch();
        if (_settings.LogGroupSums)
        {
            _settings.LogGroupSums = false;
            _context.LogGroupSums("Group Sums Buffer - Meshes");
        }
        
        if (_settings.LogScannedPredicates)
        {
            _settings.LogScannedPredicates = false;
            _context.LogScannedPredicates("Scanned Predicates - Meshes");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Thread Groups");
        _groupSumsScanningDispatcher.SubmitGroupSumsData().Dispatch();
        if (_settings.LogScannedGroupSums)
        {
            _settings.LogScannedGroupSums = false;
            _context.LogScannedGroupSums("Scanned Group Sums Buffer - Meshes");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Copy Instances Data");
        _dataCopyingDispatcher.SubmitMeshesData().Dispatch();
        if (_settings.LogCulledMatrices)
        {
            _settings.LogCulledMatrices = false;
            _context.Transforms.LogCulledMatrices("Culled Matrices - Meshes");
        }
        
        if (_settings.LogArgumentsAfterCopy)
        {
            _settings.LogArgumentsAfterCopy = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Copy");
        }
        Profiler.EndSample();
    }

    private void DrawInstances()
    {
        for (var i = 0; i < _instances.Length; i++)
        {
            var instance = _instances[i];
            var renderParams = new RenderParams(instance.Material)
            {
                worldBounds = _worldBounds,
            };

            if (!_settings.EnableLod)
            {
                RenderInstances(i, (int)instance.DefaultLod, instance, renderParams);
                continue;
            }

            for (var k = 0; k < instance.Lods.Count; k++)
            {
                RenderInstances(i, k, instance, renderParams);
            }
        }
    }

    private void RenderInstances(int instanceIndex, int lodIndex, InstanceProperties instance, RenderParams renderParams)
    {
        var lod = instance.Lods[lodIndex];
        var startCommand = instanceIndex * instance.Lods.Count + lodIndex;
        renderParams.matProps = lod.MaterialPropertyBlock;
        renderParams.shadowCastingMode = lod.CastsShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        Graphics.RenderMeshIndirect(renderParams, lod.Mesh, _context.Arguments.GraphicsBuffer, 1, startCommand);
    }

    private void InitializeMeshProperties()
    {
        foreach (var instance in _instances)
        {
            instance.Initialize();
        }
    }
}
