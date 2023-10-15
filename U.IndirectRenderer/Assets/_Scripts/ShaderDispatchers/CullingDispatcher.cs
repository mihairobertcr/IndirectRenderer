using System.Collections.Generic;
using UnityEngine;
using IndirectRendering;

public class CullingDispatcher : ComputeShaderDispatcher
{
    private static readonly int ShouldFrustumCull = Shader.PropertyToID("_ShouldFrustumCull");
    private static readonly int ShouldOcclusionCull = Shader.PropertyToID("_ShouldOcclusionCull");
    private static readonly int ShouldLod = Shader.PropertyToID("_ShouldLod");
    private static readonly int ShouldDetailCull = Shader.PropertyToID("_ShouldDetailCull");
    
    private static readonly int IsVisibleBuffer = Shader.PropertyToID("_IsVisibleBuffer");
    private static readonly int ShadowDistance = Shader.PropertyToID("_ShadowDistance");
    private static readonly int DetailCullingScreenPercentage = Shader.PropertyToID("_DetailCullingScreenPercentage");
    private static readonly int HiZTextureSize = Shader.PropertyToID("_HiZTextureSize");
    private static readonly int HiZMap = Shader.PropertyToID("_HiZMap");
    private static readonly int MvpMatrix = Shader.PropertyToID("_MvpMatrix");
    private static readonly int CameraPosition = Shader.PropertyToID("_CameraPosition");
    
    private static readonly int LodsIntervals = Shader.PropertyToID("_LodsIntervals");
    private static readonly int DefaultLods = Shader.PropertyToID("_DefaultLods");
    private static readonly int LodsCount = Shader.PropertyToID("_LodsCount");
    
    private readonly int _kernel;
    private readonly int _threadGroupX;
    
    private readonly GraphicsBuffer _argumentsBuffer;
    private readonly ComputeBuffer _visibilityBuffer;
    private readonly ComputeBuffer _defaultLodsBuffer;
    private readonly ComputeBuffer _lodsRangesBuffer;
    private readonly ComputeBuffer _boundsDataBuffer;
    private readonly ComputeBuffer _sortingDataBuffer;

    private List<uint> _defaultLods;
    private List<float> _lodsRanges;
    private List<BoundsData> _boundsData;

    public CullingDispatcher(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _kernel = GetKernel("CSMain");
        _threadGroupX = Mathf.Max(1, context.MeshesCount / 64);
        
        InitializeCullingBuffers(
            out _argumentsBuffer,
            out _visibilityBuffer,
            out _defaultLodsBuffer,
            out _lodsRangesBuffer,
            out _boundsDataBuffer,
            out _sortingDataBuffer);
    }

    public CullingDispatcher SetSettings(IndirectRendererSettings settings)
    {
        ComputeShader.SetInt(ShouldFrustumCull, settings.EnableFrustumCulling ? 1 : 0);
        ComputeShader.SetInt(ShouldOcclusionCull, settings.EnableOcclusionCulling ? 1 : 0);
        ComputeShader.SetInt(ShouldDetailCull, settings.EnableDetailCulling ? 1 : 0);
        ComputeShader.SetInt(ShouldLod, settings.EnableLod ? 1 : 0);

        ComputeShader.SetFloat(ShadowDistance, QualitySettings.shadowDistance);
        ComputeShader.SetFloat(DetailCullingScreenPercentage, settings.DetailCullingPercentage);

        return this;
    }
    
    public CullingDispatcher SetBoundsData(InstanceProperties[] meshes)
    {
        _boundsData = new List<BoundsData>();
        foreach (var mesh in meshes)
        {
            foreach (var transform in mesh.Transforms)
            {
                var bounds = mesh.Bounds;
                bounds.center += transform.Position;
                
                var size = bounds.size;
                size.Scale(transform.Scale);
                bounds.size = size;
                
                _boundsData.Add(new BoundsData
                {
                    BoundsCenter = bounds.center,
                    BoundsExtents = bounds.extents,
                });
            }
        }

        _boundsDataBuffer.SetData(_boundsData);
        return this;
    }

    public CullingDispatcher SetLodsData(InstanceProperties[] meshes)
    {
        _defaultLods = new List<uint>();
        _lodsRanges = new List<float>();
        foreach (var mesh in meshes)
        {
            _defaultLods.Add(mesh.DefaultLod);
            foreach (var lod in mesh.Lods)
            {
                _lodsRanges.Add(lod.Range);
            }
        }
        
        _defaultLodsBuffer.SetData(_defaultLods);
        _lodsRangesBuffer.SetData(_lodsRanges);

        return this;
    }

    public CullingDispatcher SetDepthMap()
    {
        ComputeShader.SetVector(HiZTextureSize, HierarchicalDepthMap.Resolution);
        ComputeShader.SetTexture(_kernel, HiZMap, HierarchicalDepthMap.Texture);
        
        return this;
    }
    
    public CullingDispatcher SubmitCullingData()
    {
        ComputeShader.SetBuffer(_kernel, ArgsBuffer, _argumentsBuffer);
        ComputeShader.SetBuffer(_kernel, IsVisibleBuffer, _visibilityBuffer);
        ComputeShader.SetBuffer(_kernel, BoundsData, _boundsDataBuffer);
        ComputeShader.SetBuffer(_kernel, SortingData, _sortingDataBuffer);

        return this;
    }

    public CullingDispatcher SubmitCameraData(Camera camera)
    {
        var cameraPosition = camera.transform.position;
        var worldMatrix = camera.worldToCameraMatrix;
        var projectionMatrix = camera.projectionMatrix;
        var modelViewProjection = projectionMatrix * worldMatrix;

        ComputeShader.SetMatrix(MvpMatrix, modelViewProjection);
        ComputeShader.SetVector(CameraPosition, cameraPosition);

        return this;
    }

    public CullingDispatcher SubmitLodsData()
    {
        ComputeShader.SetInt(LodsCount, Context.LodsCount);
        ComputeShader.SetBuffer(_kernel, LodsIntervals, _lodsRangesBuffer);
        ComputeShader.SetBuffer(_kernel, DefaultLods, _defaultLodsBuffer);
        
        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);

    // TODO: #EDITOR
    public void DrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.333f);
        for (var i = 0; i < _boundsData.Count; i++)
        {
            Gizmos.DrawWireCube(_boundsData[i].BoundsCenter, _boundsData[i].BoundsExtents * 2f);
        }
    }

    private void InitializeCullingBuffers(out GraphicsBuffer args,
        out ComputeBuffer visibility, out ComputeBuffer defaultLods, 
        out ComputeBuffer lodsRanges, out ComputeBuffer bounds, 
        out ComputeBuffer sortingData)
    {
        args = Context.Arguments.GraphicsBuffer;
        visibility = Context.Visibility;
        defaultLods = Context.DefaultLods;
        lodsRanges = Context.LodsRanges;
        bounds = Context.BoundingBoxes;
        sortingData = Context.Sorting.Data;
    }
}