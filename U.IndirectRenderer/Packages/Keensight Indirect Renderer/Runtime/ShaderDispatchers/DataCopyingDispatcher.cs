using UnityEngine;
using Keensight.Rendering.Context;

namespace Keensight.Rendering.ShaderDispatchers
{
    public class DataCopyingDispatcher : ComputeShaderDispatcher
    {
        private static readonly int ArgumentsOffsetId = Shader.PropertyToID("_ArgumentsOffset");
        private static readonly int DrawCallsCountId = Shader.PropertyToID("_DrawCallsCount");
        
        private static readonly int CulledMatrixRows01sId = Shader.PropertyToID("_CulledMatrixRows01s");
        private static readonly int CulledMatrixRows23sId = Shader.PropertyToID("_CulledMatrixRows23s");
        private static readonly int CulledMatrixRows45sId = Shader.PropertyToID("_CulledMatrixRows45s");
        
        private readonly int _kernel;
        private readonly int _threadGroupX;

        private readonly ComputeBuffer _matricesRows01;
        private readonly ComputeBuffer _matricesRows23;
        private readonly ComputeBuffer _matricesRows45;
        private readonly ComputeBuffer _sortingData;
        private readonly ComputeBuffer _boundsData;

        private readonly ComputeBuffer _visibility;
        private readonly ComputeBuffer _scannedGroupSums;
        private readonly ComputeBuffer _scannedPredicates;
        private readonly ComputeBuffer _culledMatricesRows01;
        private readonly ComputeBuffer _culledMatricesRows23;
        private readonly ComputeBuffer _culledMatricesRows45;
        
        private readonly GraphicsBuffer _arguments;

        public DataCopyingDispatcher(RendererContext context)
            : base(context.Config.DataCopying, context)
        {
            _kernel = GetKernel("CSMain");
            _threadGroupX = Mathf.Max(1, context.MeshesCount / (2 * SCAN_THREAD_GROUP_SIZE));

            InitializeCopingBuffers(
                out _matricesRows01,
                out _matricesRows23,
                out _matricesRows45,
                out _sortingData,
                out _boundsData);

            InitializeMeshesBuffer(
                out _visibility,
                out _scannedGroupSums,
                out _scannedPredicates,
                out _culledMatricesRows01,
                out _culledMatricesRows23,
                out _culledMatricesRows45,
                out _arguments);
        }

        public override ComputeShaderDispatcher Initialize()
        {
            BindMaterialProperties();
            SubmitCopingBuffers();
            SubmitMeshesData();
            
            return this;
        }

        public override void Dispatch() => ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);
        
        private void SubmitCopingBuffers()
        {
            ComputeShader.SetInt(DrawCallsCountId, Context.Arguments.MeshArgumentsCount);

            ComputeShader.SetBuffer(_kernel, MatrixRows01sId, _matricesRows01);
            ComputeShader.SetBuffer(_kernel, MatrixRows23sId, _matricesRows23);
            ComputeShader.SetBuffer(_kernel, MatrixRows45sId, _matricesRows45);
            ComputeShader.SetBuffer(_kernel, SortingDataId, _sortingData);
            ComputeShader.SetBuffer(_kernel, BoundsDataId, _boundsData);
        }

        private void BindMaterialProperties()
        {
            for (var i = 0; i < Context.MeshesProperties.Count; i++)
            {
                var property = Context.MeshesProperties[i];
                for (var k = 0; k < property.Lods.Count; k++)
                {
                    var lod = property.Lods[k];
                    var argsOffset = (i * Context.Arguments.MeshArgumentsCount) + 
                        (4 + (k * ArgumentsBuffer.ARGUMENTS_COUNT));

                    lod.MaterialPropertyBlock.SetInt(ArgumentsOffsetId, argsOffset);
                    lod.MaterialPropertyBlock.SetBuffer(ArgumentsId, _arguments);
                    lod.MaterialPropertyBlock.SetBuffer(MatrixRows01sId, _culledMatricesRows01);
                    lod.MaterialPropertyBlock.SetBuffer(MatrixRows23sId, _culledMatricesRows23);
                    lod.MaterialPropertyBlock.SetBuffer(MatrixRows45sId, _culledMatricesRows45);
                }
            }
        }

        private void SubmitMeshesData()
        {
            ComputeShader.SetBuffer(_kernel, PredicatesInputId, _visibility);
            ComputeShader.SetBuffer(_kernel, GroupSumsId, _scannedGroupSums);
            ComputeShader.SetBuffer(_kernel, ScannedPredicatesId, _scannedPredicates);
            ComputeShader.SetBuffer(_kernel, CulledMatrixRows01sId, _culledMatricesRows01);
            ComputeShader.SetBuffer(_kernel, CulledMatrixRows23sId, _culledMatricesRows23);
            ComputeShader.SetBuffer(_kernel, CulledMatrixRows45sId, _culledMatricesRows45);
            ComputeShader.SetBuffer(_kernel, ArgumentsId, _arguments);
        }

        private void InitializeCopingBuffers(out ComputeBuffer matricesRows01,
            out ComputeBuffer matricesRows23, out ComputeBuffer matricesRows45,
            out ComputeBuffer sortingData, out ComputeBuffer boundsData)
        {
            matricesRows01 = Context.Transforms.Matrices.Rows01;
            matricesRows23 = Context.Transforms.Matrices.Rows23;
            matricesRows45 = Context.Transforms.Matrices.Rows45;
            sortingData = Context.Sorting.Data;
            boundsData = Context.BoundingBoxes;
        }

        private void InitializeMeshesBuffer(out ComputeBuffer meshesVisibility,
            out ComputeBuffer meshesScannedGroupSums, out ComputeBuffer meshesScannedPredicates,
            out ComputeBuffer meshesCulledMatricesRows01, out ComputeBuffer meshesCulledMatricesRows23,
            out ComputeBuffer meshesCulledMatricesRows45, out GraphicsBuffer meshesArguments)
        {
            meshesVisibility = Context.Visibility;
            meshesScannedGroupSums = Context.ScannedGroupSums;
            meshesScannedPredicates = Context.ScannedPredicates;
            meshesCulledMatricesRows01 = Context.Transforms.CulledMatrices.Rows01;
            meshesCulledMatricesRows23 = Context.Transforms.CulledMatrices.Rows23;
            meshesCulledMatricesRows45 = Context.Transforms.CulledMatrices.Rows45;
            meshesArguments = Context.Arguments.GraphicsBuffer;
        }
    }
}