using System.Collections.Generic;
using UnityEngine;
using IndirectRendering;

public class InstancesCuller : ComputeShaderDispatcher
{
    private readonly int _kernel;
    private readonly int _threadGroupX;
    
    private readonly GraphicsBuffer _meshesArgumentsBuffer;
    private readonly GraphicsBuffer _shadowsArgumentsBuffer;
    private readonly ComputeBuffer _meshesVisibilityBuffer;
    private readonly ComputeBuffer _shadowsVisibilityBuffer;
    private readonly ComputeBuffer _boundsDataBuffer;
    private readonly ComputeBuffer _sortingDataBuffer;

    private BoundsData[] _boundsData;

    public InstancesCuller(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
    {
        _kernel = GetKernel("CSMain");
        _threadGroupX = Mathf.Max(1, context.MeshesCount / 64);
        
        InitializeCullingBuffers(
            out _meshesArgumentsBuffer,
            out _shadowsArgumentsBuffer,
            out _meshesVisibilityBuffer,
            out _shadowsVisibilityBuffer,
            out _boundsDataBuffer,
            out _sortingDataBuffer);
    }

    public InstancesCuller SetSettings(IndirectRendererSettings settings)
    {
        ComputeShader.SetInt(ShaderProperties.ShouldFrustumCull, settings.EnableFrustumCulling ? 1 : 0);
        ComputeShader.SetInt(ShaderProperties.ShouldOcclusionCull, settings.EnableOcclusionCulling ? 1 : 0);
        ComputeShader.SetInt(ShaderProperties.ShouldDetailCull, settings.EnableDetailCulling ? 1 : 0);
        ComputeShader.SetInt(ShaderProperties.ShouldLod, settings.EnableLod ? 1 : 0);
        ComputeShader.SetInt(ShaderProperties.ShouldOnlyUseLod2Shadows, settings.EnableOnlyLod2Shadows ? 1 : 0);

        ComputeShader.SetFloat(ShaderProperties.ShadowDistance, QualitySettings.shadowDistance);
        ComputeShader.SetFloat(ShaderProperties.DetailCullingScreenPercentage, settings.DetailCullingPercentage);

        return this;
    }
    
    public InstancesCuller SetBoundsData(Vector3[] positions, Vector3[] scales)
    {
        _boundsData = new BoundsData[Context.MeshesCount];
        for (var i = 0; i < positions.Length; i++)
        {
            //TODO: Create bounding boxes
            var bounds = new Bounds();
            bounds.center = positions[i];
            var size = Vector3.one; // TODO: Properly calculate or pass the size of aabbs
            size.Scale(scales[i]);
            bounds.size = size;

            _boundsData[i] = new BoundsData
            {
                BoundsCenter = bounds.center,
                BoundsExtents = bounds.extents,
            };
        }
        
        _boundsDataBuffer.SetData(_boundsData);
        return this;
    }

    public InstancesCuller SetDepthMap(HierarchicalDepthMap depthMap)
    {
        ComputeShader.SetVector(ShaderProperties.HiZTextureSize, depthMap.TextureSize);
        ComputeShader.SetTexture(_kernel, ShaderProperties.HiZMap, depthMap.Texture);
        
        return this;
    }
    
    public void SubmitCullingData()
    {
        ComputeShader.SetBuffer(_kernel, ShaderProperties.ArgsBuffer, _meshesArgumentsBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.ShadowArgsBuffer, _shadowsArgumentsBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.IsVisibleBuffer, _meshesVisibilityBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.IsShadowVisibleBuffer, _shadowsVisibilityBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.BoundsData, _boundsDataBuffer);
        ComputeShader.SetBuffer(_kernel, ShaderProperties.SortingData, _sortingDataBuffer);
    }

    public InstancesCuller SubmitCameraData(Camera camera)
    {
        var cameraPosition = camera.transform.position;
        var worldMatrix = camera.worldToCameraMatrix;
        var projectionMatrix = camera.projectionMatrix;
        var modelViewProjection = projectionMatrix * worldMatrix;

        ComputeShader.SetMatrix(ShaderProperties.MvpMatrix, modelViewProjection);
        ComputeShader.SetVector(ShaderProperties.CameraPosition, cameraPosition);
        
        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);

    // TODO: #EDITOR
    public void DrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.333f);
        for (var i = 0; i < _boundsData.Length; i++)
        {
            Gizmos.DrawWireCube(_boundsData[i].BoundsCenter, _boundsData[i].BoundsExtents * 2f);
        }
    }

    private void InitializeCullingBuffers(out GraphicsBuffer meshesArgs, out GraphicsBuffer shadowsArgs, 
        out ComputeBuffer meshesVisibility, out ComputeBuffer shadowsVisibility, 
        out ComputeBuffer bounds, out ComputeBuffer sortingData)
    {
        meshesArgs = Context.Arguments.MeshesBuffer;
        shadowsArgs = Context.Arguments.ShadowsBuffer;
        meshesVisibility = Context.Visibility.Meshes;
        shadowsVisibility = Context.Visibility.Shadows;
        bounds = Context.BoundsData;
        sortingData = Context.Sorting.Data;
    }
}