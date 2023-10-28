using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class IndirectRenderer : IDisposable
{
    private readonly Camera _camera;
    private readonly List<InstanceProperties> _instances;
    private readonly RendererConfig _config;
    private readonly RendererDataContext _context;

    private readonly MatricesInitDispatcher _matricesInitDispatcher;
    private readonly LodsSortingDispatcher _lodsSortingDispatcher;
    private readonly CullingDispatcher _cullingDispatcher;
    private readonly PredicatesScanningDispatcher _predicatesScanningDispatcher;
    private readonly GroupSumsScanningDispatcher _groupSumsScanningDispatcher;
    private readonly DataCopyingDispatcher _dataCopyingDispatcher;

    private Bounds _worldBounds;
    
    public IndirectRenderer(Camera camera, RendererConfig config, List<InstanceProperties> instances)
    {
        _camera = camera;
        _instances = instances;
        _config = config;
        
        InitializeMeshProperties();
        _worldBounds.extents = Vector3.one * 10000; // ???
        
        _context = new RendererDataContext(config, _instances);

        _matricesInitDispatcher = new MatricesInitDispatcher(_config.MatricesInit, _context);
        _lodsSortingDispatcher = new LodsSortingDispatcher(_config.LodSorting, _context);
        _cullingDispatcher = new CullingDispatcher(_config.VisibilityCulling, _context);
        _predicatesScanningDispatcher = new PredicatesScanningDispatcher(_config.PredicatesScanning, _context);
        _groupSumsScanningDispatcher = new GroupSumsScanningDispatcher(_config.GroupSumsScanning, _context);
        _dataCopyingDispatcher = new DataCopyingDispatcher(_config.DataCopying, _context);

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
        if (_config.Debugger.DrawBounds)
        {
            _cullingDispatcher.DrawGizmos();
        }
    }
    
    private void Initialize()
    {
        _matricesInitDispatcher
            .SetTransformData(_instances)
            .SubmitTransformsData()
            .Dispatch();

        _lodsSortingDispatcher
            .SetSortingData(_instances, _camera)
            .SetupSortingCommand()
            .EnabledAsyncComputing(true);
        
        _cullingDispatcher
            .SetSettings(_config)
            .SetBoundsData(_instances)
            .SetLodsData(_instances)
            .SetDepthMap()
            .SubmitCullingData()
            .SubmitLodsData();

        _predicatesScanningDispatcher
            .SubmitMeshesData();
        
        _groupSumsScanningDispatcher
            .SubmitGroupCount()
            .SubmitGroupSumsData();
        
        _dataCopyingDispatcher
            .BindMaterialProperties(_instances)
            .SubmitCopingBuffers()
            .SubmitMeshesData();
    }

    private void BeginFrameRendering(ScriptableRenderContext context, Camera[] camera)
    {
        Profiler.BeginSample("CalculateVisibleInstances");
        CalculateVisibleInstances();
        Profiler.EndSample();
        
        Profiler.BeginSample("DrawMeshes");
        RenderInstances();
        Profiler.EndSample();
    }

    private void CalculateVisibleInstances()
    {
        _worldBounds.center = _camera.transform.position;
        
        if (_config.Debugger.LogMatrices)
        {
            _config.Debugger.LogMatrices = false;
            _context.Transforms.LogMatrices("Matrices");
        }
        
        Profiler.BeginSample("Resetting Args Buffer");
        _context.Arguments.Reset();
        if (_config.Debugger.LogArgumentsAfterReset)
        {
            _config.Debugger.LogArgumentsAfterReset = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Reset");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("LOD Sorting");
        _lodsSortingDispatcher.Dispatch();
        if (_config.Debugger.LogSortingData)
        {
            _config.Debugger.LogSortingData = false;
            _context.Sorting.Log("Sorting Data");
        }
        Profiler.EndSample();

        Profiler.BeginSample("Occlusion");
        _cullingDispatcher
            .SubmitCameraData(_camera)
            .Dispatch();
        
        if (_config.Debugger.LogArgumentsAfterOcclusion)
        {
            _config.Debugger.LogArgumentsAfterOcclusion = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Occlusion");
        }
        
        if (_config.Debugger.LogVisibilityBuffer)
        {
            _config.Debugger.LogVisibilityBuffer = false;
            _context.LogVisibility("Visibility Buffers - Meshes");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Instances");
        _predicatesScanningDispatcher.Dispatch();
        if (_config.Debugger.LogGroupSums)
        {
            _config.Debugger.LogGroupSums = false;
            _context.LogGroupSums("Group Sums Buffer - Meshes");
        }
        
        if (_config.Debugger.LogScannedPredicates)
        {
            _config.Debugger.LogScannedPredicates = false;
            _context.LogScannedPredicates("Scanned Predicates - Meshes");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Scan Thread Groups");
        _groupSumsScanningDispatcher.Dispatch();
        if (_config.Debugger.LogScannedGroupSums)
        {
            _config.Debugger.LogScannedGroupSums = false;
            _context.LogScannedGroupSums("Scanned Group Sums Buffer - Meshes");
        }
        Profiler.EndSample();
        
        Profiler.BeginSample("Copy Instances Data");
        _dataCopyingDispatcher.Dispatch();
        if (_config.Debugger.LogCulledMatrices)
        {
            _config.Debugger.LogCulledMatrices = false;
            _context.Transforms.LogCulledMatrices("Culled Matrices - Meshes");
        }
        
        if (_config.Debugger.LogArgumentsAfterCopy)
        {
            _config.Debugger.LogArgumentsAfterCopy = false;
            _context.Arguments.Log("Arguments Buffers - Meshes After Copy");
        }
        Profiler.EndSample();
    }

    private void RenderInstances()
    {
        for (var i = 0; i < _instances.Count; i++)
        {
            var instance = _instances[i];
            var renderParams = new RenderParams(instance.Material)
            {
                worldBounds = _worldBounds,
            };
    
            if (!_config.EnableLod)
            {
                DrawInstances(i, (int)instance.DefaultLod, instance, renderParams);
                continue;
            }
    
            for (var k = 0; k < instance.Lods.Count; k++)
            {
                DrawInstances(i, k, instance, renderParams);
            }
        }
    }

    private void DrawInstances(int instanceIndex, int lodIndex, InstanceProperties instance, RenderParams renderParams)
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
