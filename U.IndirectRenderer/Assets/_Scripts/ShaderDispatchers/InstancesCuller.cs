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

    private List<BoundsData> _boundsData;

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
    
    //TODO: Enforce that this should be called only in initialization faze
    public InstancesCuller SetBoundsData(IndirectMesh[] meshes)
    {
        _boundsData = new List<BoundsData>();
        for (var i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            for (var k = 0; k < Context.MeshesCount; k++)
            {
                var bounds = CreateBounds(mesh.Prefab);
                bounds.center = mesh.Positions[k];
                var size = bounds.size;
                size.Scale(mesh.Scales[k]);
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

    public InstancesCuller SetDepthMap()
    {
        ComputeShader.SetVector(ShaderProperties.HiZTextureSize, HierarchicalDepthMap.Resolution);
        ComputeShader.SetTexture(_kernel, ShaderProperties.HiZMap, HierarchicalDepthMap.Texture);
        
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
    // TODO: Check if debug boxes are rendered correctly
    public void DrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.333f);
        for (var i = 0; i < _boundsData.Count; i++)
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

    private Bounds CreateBounds(GameObject prefab)
    {
        var gameObject = Object.Instantiate(prefab);
        gameObject.transform.position = Vector3.zero;
        gameObject.transform.rotation = Quaternion.Euler(Vector3.zero);
        gameObject.transform.localScale = Vector3.one;
        
        var renderers = gameObject.GetComponentsInChildren<Renderer>();
        
        var bounds = new Bounds();
        if (renderers.Length > 0)
        {
            bounds = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);
            for (var r = 1; r < renderers.Length; r++)
            {
                bounds.Encapsulate(renderers[r].bounds);
            }
        }
        
        bounds.center = Vector3.zero;
        Object.DestroyImmediate(gameObject);
        
        return bounds;
    }
}