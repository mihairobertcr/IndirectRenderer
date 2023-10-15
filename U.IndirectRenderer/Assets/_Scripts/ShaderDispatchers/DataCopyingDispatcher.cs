using UnityEngine;

public class DataCopyingDispatcher : ComputeShaderDispatcher
{
    private static readonly int ArgsOffset = Shader.PropertyToID("_ArgsOffset");
    
    private static readonly int CulledMatrixRows01 = Shader.PropertyToID("_CulledMatrixRows01");
    private static readonly int CulledMatrixRows23 = Shader.PropertyToID("_CulledMatrixRows23");
    private static readonly int CulledMatrixRows45 = Shader.PropertyToID("_CulledMatrixRows45");
    
    private static readonly int NumberOfDrawCalls = Shader.PropertyToID("_NumberOfDrawCalls");
    private static readonly int DrawCallsDataOutput = Shader.PropertyToID("_DrawCallsDataOutput");
    
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

    public DataCopyingDispatcher(ComputeShader computeShader, RendererDataContext context)
        : base(computeShader, context)
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

    public DataCopyingDispatcher SubmitCopingBuffers()
    {
        ComputeShader.SetInt(NumberOfDrawCalls, Context.Arguments.InstanceArgumentsCount);

        ComputeShader.SetBuffer(_kernel, MatrixRows01, _matricesRows01);
        ComputeShader.SetBuffer(_kernel, MatrixRows23, _matricesRows23);
        ComputeShader.SetBuffer(_kernel, MatrixRows45, _matricesRows45);
        ComputeShader.SetBuffer(_kernel, SortingData, _sortingData);
        ComputeShader.SetBuffer(_kernel, BoundsData, _boundsData);

        return this;
    }

    public DataCopyingDispatcher BindMaterialProperties(InstanceProperties[] properties)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            for (var k = 0; k < property.Lods.Count; k++)
            {
                var lod = property.Lods[k];
                var argsOffset = (i * Context.Arguments.InstanceArgumentsCount) + 
                    (4 + (k * ArgumentsBuffer.ARGUMENTS_COUNT));

                lod.MaterialPropertyBlock.SetInt(ArgsOffset, argsOffset);
                lod.MaterialPropertyBlock.SetBuffer(ArgsBuffer, _arguments);
                lod.MaterialPropertyBlock.SetBuffer(MatrixRows01, _culledMatricesRows01);
                lod.MaterialPropertyBlock.SetBuffer(MatrixRows23, _culledMatricesRows23);
                lod.MaterialPropertyBlock.SetBuffer(MatrixRows45, _culledMatricesRows45);
            }
        }

        return this;
    }

    public DataCopyingDispatcher SubmitMeshesData()
    {
        ComputeShader.SetBuffer(_kernel, PredicatesInput, _visibility);
        ComputeShader.SetBuffer(_kernel, GroupSums, _scannedGroupSums);
        ComputeShader.SetBuffer(_kernel, ScannedPredicates, _scannedPredicates);
        ComputeShader.SetBuffer(_kernel, CulledMatrixRows01, _culledMatricesRows01);
        ComputeShader.SetBuffer(_kernel, CulledMatrixRows23, _culledMatricesRows23);
        ComputeShader.SetBuffer(_kernel, CulledMatrixRows45, _culledMatricesRows45);
        ComputeShader.SetBuffer(_kernel, DrawCallsDataOutput, _arguments);

        return this;
    }

    public override void Dispatch() => ComputeShader.Dispatch(_kernel, _threadGroupX, 1, 1);

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