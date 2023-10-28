using System.Collections.Generic;
using UnityEngine;
using IndirectRendering;

public class CullingDispatcher : ComputeShaderDispatcher
{
    private static readonly int EnableFrustumCullingId = Shader.PropertyToID("_EnableFrustumCulling");
    private static readonly int EnableOcclusionCullingId = Shader.PropertyToID("_EnableOcclusionCulling");
    private static readonly int EnableDetailCullingId = Shader.PropertyToID("_EnableDetailCulling");
    private static readonly int EnableLodsId = Shader.PropertyToID("_EnableLods");
    
    private static readonly int DetailCullingScreenPercentageId = Shader.PropertyToID("_DetailCullingScreenPercentage");
    private static readonly int DepthMapResolutionId = Shader.PropertyToID("_DepthMapResolution");
    private static readonly int CameraPositionId = Shader.PropertyToID("_CameraPosition");
    private static readonly int LodsCountId = Shader.PropertyToID("_LodsCount");
    private static readonly int MvpMatrixId = Shader.PropertyToID("_MvpMatrix");
    private static readonly int DepthMapId = Shader.PropertyToID("_DepthMap");
    
    private static readonly int VisibilityBufferId = Shader.PropertyToID("_VisibilityBuffer");
    private static readonly int LodsRangesBufferId = Shader.PropertyToID("_LodsRangesBuffer");
    private static readonly int DefaultLodsBufferId = Shader.PropertyToID("_DefaultLodsBuffer");
    
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

    public CullingDispatcher SetSettings(RendererConfig config)
    {
        ComputeShader.SetInt(EnableFrustumCullingId, config.EnableFrustumCulling ? 1 : 0);
        ComputeShader.SetInt(EnableOcclusionCullingId, config.EnableOcclusionCulling ? 1 : 0);
        ComputeShader.SetInt(EnableDetailCullingId, config.EnableDetailCulling ? 1 : 0);
        ComputeShader.SetInt(EnableLodsId, config.EnableLod ? 1 : 0);
        
        ComputeShader.SetFloat(DetailCullingScreenPercentageId, config.DetailCullingPercentage);

        return this;
    }
    
    public CullingDispatcher SetBoundsData(List<InstanceProperties> meshes)
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

    public CullingDispatcher SetLodsData(List<InstanceProperties> meshes)
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
        ComputeShader.SetVector(DepthMapResolutionId, HierarchicalDepthMap.Resolution);
        ComputeShader.SetTexture(_kernel, DepthMapId, HierarchicalDepthMap.Texture);
        
        return this;
    }
    
    public CullingDispatcher SubmitCullingData()
    {
        ComputeShader.SetBuffer(_kernel, ArgsBufferId, _argumentsBuffer);
        ComputeShader.SetBuffer(_kernel, VisibilityBufferId, _visibilityBuffer);
        ComputeShader.SetBuffer(_kernel, BoundsDataId, _boundsDataBuffer);
        ComputeShader.SetBuffer(_kernel, SortingDataId, _sortingDataBuffer);

        return this;
    }

    public CullingDispatcher SubmitCameraData(Camera camera)
    {
        var cameraPosition = camera.transform.position;
        var worldMatrix = camera.worldToCameraMatrix;
        var projectionMatrix = camera.projectionMatrix;
        var modelViewProjection = projectionMatrix * worldMatrix;

        ComputeShader.SetMatrix(MvpMatrixId, modelViewProjection);
        ComputeShader.SetVector(CameraPositionId, cameraPosition);

        return this;
    }

    public CullingDispatcher SubmitLodsData()
    {
        ComputeShader.SetInt(LodsCountId, Context.LodsCount);
        ComputeShader.SetBuffer(_kernel, LodsRangesBufferId, _lodsRangesBuffer);
        ComputeShader.SetBuffer(_kernel, DefaultLodsBufferId, _defaultLodsBuffer);
        
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