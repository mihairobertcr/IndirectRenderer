using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using Unity.Profiling;

using Keensight.Rendering.Configs;
using Keensight.Rendering.Context;
using Keensight.Rendering.Dispatchers;

namespace Keensight.Rendering
{
    public class IndirectRenderer : IDisposable
    {
        private readonly Camera _camera;
        private readonly RendererConfig _config;
        private readonly RendererContext _context;
        private readonly List<MeshProperties> _instances;

        private readonly MatricesInitDispatcher _matricesInitDispatcher;
        private readonly LodsSortingDispatcher _lodsSortingDispatcher;
        private readonly VisibilityCullingDispatcher _visibilityCullingDispatcher;
        private readonly PredicatesScanningDispatcher _predicatesScanningDispatcher;
        private readonly GroupSumsScanningDispatcher _groupSumsScanningDispatcher;
        private readonly DataCopyingDispatcher _dataCopyingDispatcher;

        private Bounds _worldBounds;
        
        public IndirectRenderer(Camera camera, RendererConfig config, List<MeshProperties> instances)
        {
            _camera = camera;
            _instances = instances;
            _config = config;
            
            InitializeMeshProperties();
            _worldBounds.extents = Vector3.one * 10000; // ???
            
            _context = new RendererContext(config, _instances, _camera);

            _matricesInitDispatcher = new MatricesInitDispatcher(_context);
            _lodsSortingDispatcher = new LodsSortingDispatcher(_context);
            _visibilityCullingDispatcher = new VisibilityCullingDispatcher(_context);
            _predicatesScanningDispatcher = new PredicatesScanningDispatcher(_context);
            _groupSumsScanningDispatcher = new GroupSumsScanningDispatcher(_context);
            _dataCopyingDispatcher = new DataCopyingDispatcher(_context);

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
                _visibilityCullingDispatcher.DrawGizmos();
            }
        }
        
        private void Initialize()
        {
            _matricesInitDispatcher.Initialize().Dispatch();
            _lodsSortingDispatcher.Initialize();
            _visibilityCullingDispatcher.Initialize();
            _predicatesScanningDispatcher.Initialize();
            _groupSumsScanningDispatcher.Initialize();
            _dataCopyingDispatcher.Initialize();
        }

        private void BeginFrameRendering(ScriptableRenderContext context, Camera[] camera)
        {
            CalculateVisibleMeshes();
            RenderMeshes();
        }

        private void CalculateVisibleMeshes()
        {
            using var profiler = new ProfilerMarker(ProfilerCategory.Render, "01.CalculateVisibleMeshes").Auto();

            _worldBounds.center = _camera.transform.position;
            LogMatrices();

            ResetArgumentsBuffer();
            SortLods();
            CalculateVisibilityCulling();
            ScanPredicates();
            ScanGroupSums();
            CopyMeshesData();
        }

        private void RenderMeshes()
        {
            using var profiler = new ProfilerMarker(ProfilerCategory.Render, "02.RenderIndirectMeshes").Auto();

            for (var i = 0; i < _instances.Count; i++)
            {
                var instance = _instances[i];
                var renderParams = new RenderParams(instance.Material)
                {
                    worldBounds = _worldBounds,
                };
        
                if (!_config.EnableLod)
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

        private void RenderInstances(int instanceIndex, int lodIndex, MeshProperties mesh, RenderParams renderParams)
        {
            var lod = mesh.Lods[lodIndex];
            var startCommand = instanceIndex * mesh.Lods.Count + lodIndex;
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

        private void ResetArgumentsBuffer()
        {
            using var profiler = new ProfilerMarker(ProfilerCategory.Render, "01.ResetArgumentsBuffer").Auto();
            
            _context.Arguments.Reset();
            LogArgumentsAfterReset();
        }

        private void SortLods()
        {
            using var profiler = new ProfilerMarker(ProfilerCategory.Render, "02.SortLods").Auto();

            _lodsSortingDispatcher.Dispatch();
            LogSortingData();
        }

        private void CalculateVisibilityCulling()
        {
            using var profiler = new ProfilerMarker(ProfilerCategory.Render, "03.Culling").Auto();

            _visibilityCullingDispatcher.Update().Dispatch();
            LogArgumentsBufferAfterCulling();
            LogVisibilityBuffer();
        }
        
        private void ScanPredicates()
        {
            using var profiler = new ProfilerMarker(ProfilerCategory.Render, "04.ScanPredicates").Auto();

            _predicatesScanningDispatcher.Dispatch();
            LogScannedPredicates();
            LogLogGroupSums();
        }

        private void ScanGroupSums()
        {
            using var profiler = new ProfilerMarker(ProfilerCategory.Render, "05.ScanGroupSums").Auto();

            _groupSumsScanningDispatcher.Dispatch();
            LogScannedGroupSums();
        }

        private void CopyMeshesData()
        {
            using var profiler = new ProfilerMarker(ProfilerCategory.Render, "06.CopyMeshesData").Auto();

            _dataCopyingDispatcher.Dispatch();
            LogCulledMatrices();
            LogArgumentsAfterCopy();
        }

        private void LogMatrices()
        {
            if (!_config.Debugger.LogMatrices) return;
            
            _config.Debugger.LogMatrices = false;
            _context.Transforms.LogMatrices("Matrices");
        }

        private void LogArgumentsAfterReset()
        {
            if (!_config.Debugger.LogArgumentsAfterReset) return;
            
            _config.Debugger.LogArgumentsAfterReset = false;
            _context.Arguments.Log("Arguments Buffers - After Reset");
        }
        
        private void LogSortingData()
        {
            if (!_config.Debugger.LogSortingData) return;
            
            _config.Debugger.LogSortingData = false;
            _context.Sorting.Log("Sorting Data");
        }

        private void LogArgumentsBufferAfterCulling()
        {
            if (!_config.Debugger.LogArgumentsAfterCulling) return;
            
            _config.Debugger.LogArgumentsAfterCulling = false;
            _context.Arguments.Log("Arguments Buffers - After Culling");
        }

        private void LogVisibilityBuffer()
        {
            if (!_config.Debugger.LogVisibilityBuffer) return;
            
            _config.Debugger.LogVisibilityBuffer = false;
            _context.LogVisibility("Visibility Buffers");
        }

        private void LogScannedPredicates()
        {
            if (!_config.Debugger.LogScannedPredicates) return;
            
            _config.Debugger.LogScannedPredicates = false;
            _context.LogScannedPredicates("Scanned Predicates");
        }

        private void LogLogGroupSums()
        {
            if (!_config.Debugger.LogGroupSums) return;
            
            _config.Debugger.LogGroupSums = false;
            _context.LogGroupSums("Group Sums Buffer");
        }

        private void LogScannedGroupSums()
        {
            if (!_config.Debugger.LogScannedGroupSums) return;
            
            _config.Debugger.LogScannedGroupSums = false;
            _context.LogScannedGroupSums("Scanned Group Sums");
        }

        private void LogCulledMatrices()
        {
            if (!_config.Debugger.LogCulledMatrices) return;
            
            _config.Debugger.LogCulledMatrices = false;
            _context.Transforms.LogCulledMatrices("Culled Matrices");
        }

        private void LogArgumentsAfterCopy()
        {
            if (!_config.Debugger.LogArgumentsAfterCopy) return;
            
            _config.Debugger.LogArgumentsAfterCopy = false;
            _context.Arguments.Log("Arguments Buffers - After Copy");
        }
    }
}

