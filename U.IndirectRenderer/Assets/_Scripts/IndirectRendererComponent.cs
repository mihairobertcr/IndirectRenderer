using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

public class IndirectRendererComponent : MonoBehaviour
{
    private const int SCAN_THREAD_GROUP_SIZE = 64;

    [SerializeField] private Mesh _mesh;
    [SerializeField] private Material _material;

    // Compute Shader
    [SerializeField] private ComputeShader _matricesInitializer;
    
    // Kernel ID's
    private int _matricesInitializerKernelID;
    
    // Compute Buffers
    private ComputeBuffer _instanceMatrixRows01;
    private ComputeBuffer _instanceMatrixRows23;
    private ComputeBuffer _instanceMatrixRows45;
    
    private ComputeBuffer _instancesArgsBuffer;
    
    // Shader Property ID's
    private static readonly int _Positions = Shader.PropertyToID("_Positions");
    private static readonly int _Scales = Shader.PropertyToID("_Scales");
    private static readonly int _Rotations = Shader.PropertyToID("_Rotations");
    
    private static readonly int _InstanceMatrixRows01 = Shader.PropertyToID("_InstanceMatrixRows01");
    private static readonly int _InstanceMatrixRows23 = Shader.PropertyToID("_InstanceMatrixRows23");
    private static readonly int _InstanceMatrixRows45 = Shader.PropertyToID("_InstanceMatrixRows45");
    
    private int _numberOfInstances = 16384;
    private int _numberOfInstanceTypes;
    
    ComputeBuffer debug;


    private void Start()
    {
        var positions = new List<Vector3>();
        var scales = new List<Vector3>();
        var rotations = new List<Vector3>();
        
        //TODO: Look into thread allocation
        for (var i = 0; i < 128; i++)
        {
            for (var j = 0; j < 128; j++)
            {
                positions.Add(new Vector3
                {
                    x = i,
                    y = .5f,
                    z = j
                });
                
                rotations.Add(new Vector3
                {
                    x = 0f,
                    y = 0f,
                    z = 0f
                });
                
                scales.Add(new Vector3
                {
                    x = .75f,
                    y = .75f,
                    z = .75f
                });
            }
        }
        
        // Argument buffer used by DrawMeshInstancedIndirect.
        var args = new uint[5] { 0, 0, 0, 0, 0 };
        
        // Arguments for drawing Mesh.
        // 0 == number of triangle indices, 1 == population,
        // others are only relevant if drawing submeshes.
        args[0] = _mesh.GetIndexCount(0);
        args[1] = (uint)_numberOfInstances;
        args[2] = _mesh.GetIndexStart(0);
        args[3] = _mesh.GetBaseVertex(0);
        
        var size = args.Length * sizeof(uint);
        
        _instancesArgsBuffer = new ComputeBuffer(_numberOfInstances, size, ComputeBufferType.IndirectArguments);
        
        _instanceMatrixRows01 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        _instanceMatrixRows23 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        _instanceMatrixRows45 = new ComputeBuffer(_numberOfInstances, Indirect2x2Matrix.Size, ComputeBufferType.IndirectArguments);
        
        _instancesArgsBuffer.SetData(args);
        
        

        TryGetKernels();
        
        var positionsBuffer = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        var scalesBuffer = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        var rotationsBuffer = new ComputeBuffer(_numberOfInstances, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        
        positionsBuffer.SetData(positions);
        scalesBuffer.SetData(scales);
        rotationsBuffer.SetData(rotations);
        
        //TODO: Set up compute shaders
        
        _matricesInitializer.SetBuffer(_matricesInitializerKernelID, _Positions, positionsBuffer);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelID, _Scales, scalesBuffer);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelID, _Rotations, rotationsBuffer);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelID, _InstanceMatrixRows01, _instanceMatrixRows01);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelID, _InstanceMatrixRows23, _instanceMatrixRows23);
        _matricesInitializer.SetBuffer(_matricesInitializerKernelID, _InstanceMatrixRows45, _instanceMatrixRows45);
        // _matricesInitializer.SetBuffer(_matricesInitializerKernelID, "_Color", debug);
        
        var groupX = Mathf.Max(1, _numberOfInstances / (2 * SCAN_THREAD_GROUP_SIZE));
        _matricesInitializer.Dispatch(_matricesInitializerKernelID, groupX, 1, 1);
        
        positionsBuffer?.Release();
        rotationsBuffer?.Release();
        scalesBuffer?.Release();
        
        _material.SetBuffer(_InstanceMatrixRows01, _instanceMatrixRows01);
        _material.SetBuffer(_InstanceMatrixRows23, _instanceMatrixRows23);
        _material.SetBuffer(_InstanceMatrixRows45, _instanceMatrixRows45);
        // _material.SetBuffer("_Color", debug);

        LogInstanceDrawMatrices();
    }

    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(
            mesh: _mesh,
            submeshIndex: 0,
            material: _material,
            bounds: new Bounds(Vector3.zero, Vector3.one * 1000),
            bufferWithArgs: _instancesArgsBuffer,
            castShadows: ShadowCastingMode.On,
            receiveShadows: true);
        // camera: Camera.main);   
    }

    private void OnDestroy()
    {
        _instancesArgsBuffer?.Release();
        _instanceMatrixRows01?.Release();
        _instanceMatrixRows23?.Release();
        _instanceMatrixRows45?.Release();
        debug?.Release();
    }

    private bool TryGetKernels() =>
        TryGetKernel("CSMain", _matricesInitializer, out _matricesInitializerKernelID); //&& 
    // TryGetKernel("BitonicSort",       _lodBitonicSorter,        out _lodSorterKernelID) && 
    // TryGetKernel("MatrixTranspose",   _lodBitonicSorter,        out _lodTransposeSorterKernelID) && 
    // TryGetKernel("CSMain",            _culler,                  out _cullerKernelID) && 
    // TryGetKernel("CSMain",            _instancesScanner,        out _instancesScannerKernelID) && 
    // TryGetKernel("CSMain",            _groupSumsScanner,        out _groupSumsScannerKernelID) && 
    // TryGetKernel("CSMain",            _instanceDataCopier,      out _instanceDataCopierKernelID);
    
    private static bool TryGetKernel(string kernelName, ComputeShader computeShader, out int kernelId)
    {
        kernelId = default;
        if (!computeShader.HasKernel(kernelName))
        {
            Debug.LogError($"{kernelName} kernel not found in {computeShader.name}!");
            return false;
        }
        
        kernelId = computeShader.FindKernel(kernelName);
        return true;
    }
    
    private void LogInstanceDrawMatrices(string prefix = "")
    {
        var matrix1 = new Indirect2x2Matrix[_numberOfInstances];
        var matrix2 = new Indirect2x2Matrix[_numberOfInstances];
        var matrix3 = new Indirect2x2Matrix[_numberOfInstances];
        _instanceMatrixRows01.GetData(matrix1);
        _instanceMatrixRows23.GetData(matrix2);
        _instanceMatrixRows45.GetData(matrix3);
        
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(prefix))
        {
            stringBuilder.AppendLine(prefix);
        }
        
        for (var i = 0; i < matrix1.Length; i++)
        {
            stringBuilder.AppendLine(
                i + "\n" 
                  + matrix1[i].FirstRow + "\n"
                  + matrix1[i].SecondRow + "\n"
                  + matrix2[i].FirstRow + "\n"
                  + "\n\n"
                  + matrix2[i].SecondRow + "\n"
                  + matrix3[i].FirstRow + "\n"
                  + matrix3[i].SecondRow + "\n"
                  + "\n"
            );
        }

        Debug.Log(stringBuilder.ToString());
    }
}
