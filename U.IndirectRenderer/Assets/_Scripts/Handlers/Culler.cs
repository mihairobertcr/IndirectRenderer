using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Culler
{
    private readonly ComputeShader _computeShader;
    private readonly int _numberOfInstances;
    
    private readonly Camera _camera;
    private readonly IndirectRendererSettings _settings;
    private readonly HiZBuffer _hiZBuffer;
    private readonly int _occlusionGroupX;

    public Culler(ComputeShader computeShader, int numberOfInstances, 
        IndirectRendererSettings settings, HiZBufferConfig hiZBufferConfig, 
        Camera camera, Camera debugCamera = null)
    {
        _computeShader = computeShader;
        _camera = camera;
        _settings = settings;
        _hiZBuffer = new HiZBuffer(hiZBufferConfig, camera);
        _occlusionGroupX = Mathf.Max(1, _numberOfInstances / 64);
    }

    public void Initialize()
    {
        _computeShader.SetInt(ShaderProperties.ShouldFrustumCull,        _settings.EnableFrustumCulling    ? 1 : 0);
        _computeShader.SetInt(ShaderProperties.ShouldOcclusionCull,      _settings.EnableOcclusionCulling  ? 1 : 0);
        _computeShader.SetInt(ShaderProperties.ShouldDetailCull,         _settings.EnableDetailCulling     ? 1 : 0);
        _computeShader.SetInt(ShaderProperties.ShouldLod,                _settings.EnableLod               ? 1 : 0);
        _computeShader.SetInt(ShaderProperties.ShouldOnlyUseLod2Shadows, _settings.EnableOnlyLod2Shadows   ? 1 : 0);
        
        _computeShader.SetFloat(ShaderProperties.ShadowDistance, QualitySettings.shadowDistance);
        _computeShader.SetFloat(ShaderProperties.DetailCullingScreenPercentage, _settings.DetailCullingPercentage);
        
        _computeShader.SetVector(ShaderProperties.HiZTextureSize, _hiZBuffer.TextureSize);
        
        _computeShader.SetBuffer(ShaderKernels.Culler, ShaderProperties.ArgsBuffer,            ShaderBuffers.Args);
        _computeShader.SetBuffer(ShaderKernels.Culler, ShaderProperties.ShadowArgsBuffer,      ShaderBuffers.ShadowsArgs);
        _computeShader.SetBuffer(ShaderKernels.Culler, ShaderProperties.IsVisibleBuffer,       ShaderBuffers.IsVisible);
        _computeShader.SetBuffer(ShaderKernels.Culler, ShaderProperties.IsShadowVisibleBuffer, ShaderBuffers.IsShadowVisible);
        _computeShader.SetBuffer(ShaderKernels.Culler, ShaderProperties.BoundsData,            ShaderBuffers.BoundsData);
        _computeShader.SetBuffer(ShaderKernels.Culler, ShaderProperties.SortingData,           ShaderBuffers.SortingData);
        
        _computeShader.SetTexture(ShaderKernels.Culler, ShaderProperties.HiZMap, _hiZBuffer.Texture);
    }

    public void Dispatch()
    {
        var worldMatrix = _camera.worldToCameraMatrix;
        var projectionMatrix = _camera.projectionMatrix;
        var modelViewProjection = projectionMatrix * worldMatrix;
        var cameraPosition = _camera.transform.position;
        
        // Input
        // _computeShader.SetFloat(ShaderProperties.ShadowDistance, QualitySettings.shadowDistance);
        _computeShader.SetMatrix(ShaderProperties.MvpMatrix, modelViewProjection);
        _computeShader.SetVector(ShaderProperties.CameraPosition, cameraPosition);
        
        // Dispatch
        _computeShader.Dispatch(ShaderKernels.Culler, _occlusionGroupX, 1, 1);
    }
}
