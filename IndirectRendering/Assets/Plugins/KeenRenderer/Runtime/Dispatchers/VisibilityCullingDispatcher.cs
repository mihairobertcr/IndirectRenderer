using System.Collections.Generic;
using UnityEngine;
using Keensight.Rendering.Context;
using Keensight.Rendering.Data;
using Keensight.Rendering.HierarchicalDepthBuffer;

namespace Keensight.Rendering.Dispatchers
{
    public class VisibilityCullingDispatcher : ComputeShaderDispatcher
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

        private static readonly int VisibilitiesId = Shader.PropertyToID("_Visibilities");
        private static readonly int LodsRangesId = Shader.PropertyToID("_LodsRanges");
        private static readonly int DefaultLodsId = Shader.PropertyToID("_DefaultLods");

        private readonly int _kernel;
        private readonly int _threadGroupX;

        private readonly DepthTexture _depthTexture;
        private readonly GraphicsBuffer _argumentsBuffer;
        private readonly ComputeBuffer _visibilityBuffer;
        private readonly ComputeBuffer _defaultLodsBuffer;
        private readonly ComputeBuffer _lodsRangesBuffer;
        private readonly ComputeBuffer _boundsDataBuffer;
        private readonly ComputeBuffer _sortingDataBuffer;

        private List<uint> _defaultLods;
        private List<float> _lodsRanges;
        private List<BoundsData> _boundsData;

        public VisibilityCullingDispatcher(RendererContext context, DepthTexture depthTexture)
            : base(context.Config.VisibilityCulling, context)
        {
            _kernel = GetKernel("CSMain");
            _threadGroupX = Mathf.Max(1, context.MeshesCount / 64);
            _depthTexture = depthTexture;

            InitializeCullingBuffers(
                out _argumentsBuffer,
                out _visibilityBuffer,
                out _defaultLodsBuffer,
                out _lodsRangesBuffer,
                out _boundsDataBuffer,
                out _sortingDataBuffer);
        }

        public override ComputeShaderDispatcher Initialize()
        {
            SetSettings();
            SetBoundsData();
            SetLodsData();
            SetDepthMap();
            SubmitCullingData();
            SubmitLodsData();

            return this;
        }

        public override ComputeShaderDispatcher Update()
        {
            SubmitCameraData();

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

        private void SetSettings()
        {
            ComputeShader.SetInt(EnableFrustumCullingId, Context.Config.EnableFrustumCulling ? 1 : 0);
            ComputeShader.SetInt(EnableOcclusionCullingId, Context.Config.EnableOcclusionCulling ? 1 : 0);
            ComputeShader.SetInt(EnableDetailCullingId, Context.Config.EnableDetailCulling ? 1 : 0);
            ComputeShader.SetInt(EnableLodsId, Context.Config.EnableLod ? 1 : 0);

            ComputeShader.SetFloat(DetailCullingScreenPercentageId, Context.Config.DetailCullingPercentage);
        }

        private void SetBoundsData()
        {
            _boundsData = new List<BoundsData>();
            foreach (var mesh in Context.MeshesProperties)
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
        }

        private void SetLodsData()
        {
            _defaultLods = new List<uint>();
            _lodsRanges = new List<float>();
            foreach (var mesh in Context.MeshesProperties)
            {
                _defaultLods.Add(mesh.DefaultLod);
                foreach (var lod in mesh.Lods)
                {
                    _lodsRanges.Add(lod.Range);
                }
            }

            _defaultLodsBuffer.SetData(_defaultLods);
            _lodsRangesBuffer.SetData(_lodsRanges);
        }

        private void SetDepthMap()
        {
            ComputeShader.SetVector(DepthMapResolutionId, new Vector2(_depthTexture.Size, _depthTexture.Size));
            ComputeShader.SetTexture(_kernel, DepthMapId, _depthTexture.Component.Texture);
        }

        private void SubmitCullingData()
        {
            ComputeShader.SetBuffer(_kernel, ArgumentsId, _argumentsBuffer);
            ComputeShader.SetBuffer(_kernel, VisibilitiesId, _visibilityBuffer);
            ComputeShader.SetBuffer(_kernel, BoundsDataId, _boundsDataBuffer);
            ComputeShader.SetBuffer(_kernel, SortingDataId, _sortingDataBuffer);
        }

        private void SubmitCameraData()
        {
            var cameraPosition = Context.Camera.transform.position;
            var worldMatrix = Context.Camera.worldToCameraMatrix;
            var projectionMatrix = Context.Camera.projectionMatrix;
            var modelViewProjection = projectionMatrix * worldMatrix;

            ComputeShader.SetMatrix(MvpMatrixId, modelViewProjection);
            ComputeShader.SetVector(CameraPositionId, cameraPosition);
        }

        private void SubmitLodsData()
        {
            ComputeShader.SetInt(LodsCountId, Context.Config.NumberOfLods);
            ComputeShader.SetBuffer(_kernel, LodsRangesId, _lodsRangesBuffer);
            ComputeShader.SetBuffer(_kernel, DefaultLodsId, _defaultLodsBuffer);
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
}