using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using IndirectRendering;

public class InstancesCuller
{
    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    
    private readonly Camera _camera;
    private readonly int _occlusionGroupX;
    private List<BoundsData> _boundsData; //TODO: Convert to array

    private readonly RendererDataContext _context;

    public InstancesCuller(ComputeShader computeShader, int numberOfInstances, RendererDataContext context,
        Camera camera, Camera debugCamera = null)
    {
        _computeShader = computeShader;
        _numberOfInstances = numberOfInstances;
        _context = context;
        
        _camera = camera;
        _occlusionGroupX = Mathf.Max(1, _numberOfInstances / 64);
    }

    public void Initialize(List<Vector3> positions, List<Vector3> scales, IndirectRendererSettings settings, HierarchicalDepthBufferConfig hiZBufferConfig)
    {
        _boundsData = new List<BoundsData>();
        for (var i = 0; i < positions.Count; i++)
        {
            //TODO: Create bounding boxes
            var bounds = new Bounds();
            bounds.center = positions[i];
            var size = Vector3.one; // TODO: Properly calculate or pass the size of aabbs
            size.Scale(scales[i]);
            bounds.size = size;
                
            _boundsData.Add(new BoundsData 
            {
                BoundsCenter = bounds.center,
                BoundsExtents = bounds.extents,
            });
        }
        
        _context.BoundsData.SetData(_boundsData);

        _computeShader.SetInt(ShaderProperties.ShouldFrustumCull,        settings.EnableFrustumCulling    ? 1 : 0);
        _computeShader.SetInt(ShaderProperties.ShouldOcclusionCull,      settings.EnableOcclusionCulling  ? 1 : 0);
        _computeShader.SetInt(ShaderProperties.ShouldDetailCull,         settings.EnableDetailCulling     ? 1 : 0);
        _computeShader.SetInt(ShaderProperties.ShouldLod,                settings.EnableLod               ? 1 : 0);
        _computeShader.SetInt(ShaderProperties.ShouldOnlyUseLod2Shadows, settings.EnableOnlyLod2Shadows   ? 1 : 0);
        
        _computeShader.SetFloat(ShaderProperties.ShadowDistance, QualitySettings.shadowDistance);
        _computeShader.SetFloat(ShaderProperties.DetailCullingScreenPercentage, settings.DetailCullingPercentage);
        
        _computeShader.SetVector(ShaderProperties.HiZTextureSize, hiZBufferConfig.TextureSize);
        
        _computeShader.SetBuffer(ShaderKernels.InstancesCuller, ShaderProperties.ArgsBuffer,            _context.Arguments.Meshes);
        _computeShader.SetBuffer(ShaderKernels.InstancesCuller, ShaderProperties.ShadowArgsBuffer,      _context.Arguments.Shadows);
        _computeShader.SetBuffer(ShaderKernels.InstancesCuller, ShaderProperties.IsVisibleBuffer,       _context.Visibility.Meshes);
        _computeShader.SetBuffer(ShaderKernels.InstancesCuller, ShaderProperties.IsShadowVisibleBuffer, _context.Visibility.Shadows);
        _computeShader.SetBuffer(ShaderKernels.InstancesCuller, ShaderProperties.BoundsData,            _context.BoundsData);
        _computeShader.SetBuffer(ShaderKernels.InstancesCuller, ShaderProperties.SortingData,           _context.Sorting.Data);
        
        _computeShader.SetTexture(ShaderKernels.InstancesCuller, ShaderProperties.HiZMap, hiZBufferConfig.Texture);
    }

    public void Dispatch()
    {
        var worldMatrix = _camera.worldToCameraMatrix;
        var projectionMatrix = _camera.projectionMatrix;
        var modelViewProjection = projectionMatrix * worldMatrix;
        var cameraPosition = _camera.transform.position;
        
        // Input
        _computeShader.SetMatrix(ShaderProperties.MvpMatrix, modelViewProjection);
        _computeShader.SetVector(ShaderProperties.CameraPosition, cameraPosition);
              
        _computeShader.Dispatch(ShaderKernels.InstancesCuller, _occlusionGroupX, 1, 1);
    }
    
    // TODO: #EDITOR
    public void DrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.333f);
        for (int i = 0; i < _boundsData.Count; i++)
        {
            Gizmos.DrawWireCube(_boundsData[i].BoundsCenter, _boundsData[i].BoundsExtents * 2f);
        }
    }
}
