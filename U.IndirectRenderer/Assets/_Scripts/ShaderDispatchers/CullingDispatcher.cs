using System.Collections.Generic;
using UnityEngine;
using IndirectRendering;

public class CullingDispatcher : ComputeShaderDispatcher
{
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
        ComputeShader.SetInt(ShaderProperties.ShouldFrustumCull, settings.EnableFrustumCulling ? 1 : 0);
        ComputeShader.SetInt(ShaderProperties.ShouldOcclusionCull, settings.EnableOcclusionCulling ? 1 : 0);
        ComputeShader.SetInt(ShaderProperties.ShouldDetailCull, settings.EnableDetailCulling ? 1 : 0);
        ComputeShader.SetInt(ShaderProperties.ShouldLod, settings.EnableLod ? 1 : 0);

        ComputeShader.SetFloat(ShaderProperties.ShadowDistance, QualitySettings.shadowDistance);
        ComputeShader.SetFloat(ShaderProperties.DetailCullingScreenPercentage, settings.DetailCullingPercentage);

        return this;
    }
    
    //TODO: Enforce that this should be called only in initialization faze
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
        ComputeShader.SetVector(ShaderProperties.HiZTextureSize, HierarchicalDepthMap.Resolution);
        ComputeShader.SetTexture(_kernel, ShaderProperties.HiZMap, HierarchicalDepthMap.Texture);
        
        return this;
    }
    
    public CullingDispatcher SubmitCullingData()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.ArgsBuffer, _argumentsBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.IsVisibleBuffer, _visibilityBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.BoundsData, _boundsDataBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.SortingData, _sortingDataBuffer);

        return this;
    }

    public CullingDispatcher SubmitCameraData(Camera camera)
    {
        var cameraPosition = camera.transform.position;
        var worldMatrix = camera.worldToCameraMatrix;
        var projectionMatrix = camera.projectionMatrix;
        var modelViewProjection = projectionMatrix * worldMatrix;

        ComputeShader.SetMatrix(ShaderProperties.MvpMatrix, modelViewProjection);
        ComputeShader.SetVector(ShaderProperties.CameraPosition, cameraPosition);

        return this;
    }

    //TODO: Revisit chaining and how data works in GPU
    public CullingDispatcher SubmitLodsData()
    {
        // _defaultLodsBuffer.SetData(_defaultLods);
        // _lodsRangesBuffer.SetData(_lodsRanges);
        
        ComputeShader.SetInt(ShaderProperties.LodsCount, Context.LodsCount);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.LodsIntervals, _lodsRangesBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.DefaultLods, _defaultLodsBuffer);
        
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

    // private Bounds CreateBounds(GameObject prefab)
    // {
    //     var gameObject = Object.Instantiate(prefab);
    //     gameObject.transform.position = Vector3.zero;
    //     gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);
    //     gameObject.transform.localScale = Vector3.one;
    //     
    //     var renderers = gameObject.GetComponentsInChildren<Renderer>();
    //     
    //     var bounds = new Bounds();
    //     if (renderers.Length > 0)
    //     {
    //         bounds = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
    //         for (var r = 1; r < renderers.Length; r++)
    //         {
    //             bounds.Encapsulate(renderers[r].bounds);
    //         }
    //     }
    //     
    //     bounds.center = Vector3.zero;
    //     Object.DestroyImmediate(gameObject);
    //     
    //     return bounds;
    // }
}