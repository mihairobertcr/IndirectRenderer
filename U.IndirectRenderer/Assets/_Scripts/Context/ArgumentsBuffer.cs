using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ArgumentsBuffer : IDisposable
{
    public GraphicsBuffer MeshesBuffer { get; }
    public GraphicsBuffer ShadowsBuffer { get; }

    public const int ARGS_PER_INSTANCE_TYPE_COUNT = DRAW_CALLS_COUNT * ARGS_PER_DRAW_COUNT;
    private const int DRAW_CALLS_COUNT = 3;
    private const int ARGS_PER_DRAW_COUNT = 5;

    private readonly InstanceProperties[] _meshProperties;
    private readonly GraphicsBuffer.IndirectDrawIndexedArgs[] _parameters;

    public ArgumentsBuffer(InstanceProperties[] meshProperties)
    {
        _meshProperties = meshProperties;
        _parameters = InitializeArgumentsBuffer();

        MeshesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _meshProperties.Length * 3, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        ShadowsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _meshProperties.Length * 3, GraphicsBuffer.IndirectDrawIndexedArgs.size);

        Reset();
    }

    public void Reset()
    {
        MeshesBuffer.SetData(_parameters);
        ShadowsBuffer.SetData(_parameters);
    }

    public void Dispose()
    {
        MeshesBuffer?.Dispose();
        ShadowsBuffer?.Dispose();
    }

    //TODO: Change to GraphicsBuffer
    public void Log(string meshesPrefix = "", string shadowPrefix = "")
    {
        var meshesArgs = new uint[ARGS_PER_INSTANCE_TYPE_COUNT * _meshProperties.Length];
        var shadowArgs = new uint[ARGS_PER_INSTANCE_TYPE_COUNT * _meshProperties.Length];
        MeshesBuffer.GetData(meshesArgs);
        ShadowsBuffer.GetData(shadowArgs);
    
        var meshesLog = new StringBuilder();
        var shadowsLog = new StringBuilder();

        if (!string.IsNullOrEmpty(meshesPrefix))
        {
            meshesLog.AppendLine(meshesPrefix);
        }

        if (!string.IsNullOrEmpty(shadowPrefix))
        {
            shadowsLog.AppendLine(shadowPrefix);
        }
    
        meshesLog.AppendLine("");
        shadowsLog.AppendLine("");
    
        meshesLog.AppendLine("IndexCountPerInstance InstanceCount StartIndex BaseVertexIndex StartInstance");
        shadowsLog.AppendLine("IndexCountPerInstance InstanceCount StartIndex BaseVertexIndex StartInstance");
    
        var counter = 0;
        meshesLog.AppendLine(_meshProperties[counter].CombinedMesh.name);
        shadowsLog.AppendLine(_meshProperties[counter].CombinedMesh.name);
        for (var i = 0; i < meshesArgs.Length; i++)
        {
            meshesLog.Append($"{meshesArgs[i]} ");
            shadowsLog.Append($"{shadowArgs[i]} ");

            if ((i + 1) % 5 == 0)
            {
                meshesLog.AppendLine("");
                shadowsLog.AppendLine("");
                if ((i + 1) < meshesArgs.Length && (i + 1) % ARGS_PER_INSTANCE_TYPE_COUNT == 0)
                {
                    meshesLog.AppendLine("");
                    shadowsLog.AppendLine("");

                    counter++;
                    var properties = _meshProperties[counter];
                    var mesh = properties.CombinedMesh;
                    meshesLog.AppendLine(mesh.name);
                    shadowsLog.AppendLine(mesh.name);
                }
            }
        }
    
        Debug.Log(meshesLog.ToString());
        Debug.Log(shadowsLog.ToString());
    }

    private GraphicsBuffer.IndirectDrawIndexedArgs[] InitializeArgumentsBuffer()
    {
        var parameters = new List<GraphicsBuffer.IndirectDrawIndexedArgs>();

        foreach (var property in _meshProperties)
        {
            // parameters.Add(new GraphicsBuffer.IndirectDrawIndexedArgs
            // {
            //     indexCountPerInstance = property.CombinedMesh.GetIndexCount(0),
            //     instanceCount = 0,
            //     startIndex = property.CombinedMesh.GetIndexStart(0),
            //     baseVertexIndex = property.CombinedMesh.GetBaseVertex(0),
            //     startInstance = 0
            // });
            //
            // parameters.Add(new GraphicsBuffer.IndirectDrawIndexedArgs
            // {
            //     indexCountPerInstance = property.CombinedMesh.GetIndexCount(1),
            //     instanceCount = 0,
            //     startIndex = property.CombinedMesh.GetIndexStart(1),
            //     baseVertexIndex = property.CombinedMesh.GetBaseVertex(1),
            //     startInstance = 0
            // });
            //
            // parameters.Add(new GraphicsBuffer.IndirectDrawIndexedArgs
            // {
            //     indexCountPerInstance = property.CombinedMesh.GetIndexCount(2),
            //     instanceCount = 0,
            //     startIndex = property.CombinedMesh.GetIndexStart(2),
            //     baseVertexIndex = property.CombinedMesh.GetBaseVertex(2),
            //     startInstance = 0
            // });
            
            // parameters.Add(new GraphicsBuffer.IndirectDrawIndexedArgs
            // {
            //     indexCountPerInstance = property.Lod0Mesh.GetIndexCount(0),
            //     instanceCount = 0,
            //     startIndex = property.Lod0Mesh.GetIndexStart(0),
            //     baseVertexIndex = property.Lod0Mesh.GetBaseVertex(0),
            //     startInstance = 0
            // });
            //
            // parameters.Add(new GraphicsBuffer.IndirectDrawIndexedArgs
            // {
            //     indexCountPerInstance = property.Lod1Mesh.GetIndexCount(0),
            //     instanceCount = 0,
            //     startIndex = property.Lod1Mesh.GetIndexStart(0),
            //     baseVertexIndex = property.Lod1Mesh.GetBaseVertex(0),
            //     startInstance = 0
            // });
            //
            // parameters.Add(new GraphicsBuffer.IndirectDrawIndexedArgs
            // {
            //     indexCountPerInstance = property.Lod2Mesh.GetIndexCount(0),
            //     instanceCount = 0,
            //     startIndex = property.Lod2Mesh.GetIndexStart(0),
            //     baseVertexIndex = property.Lod2Mesh.GetBaseVertex(0),
            //     startInstance = 0
            // });   
            
            foreach (var lod in property.Lods)
            {
                parameters.Add(new GraphicsBuffer.IndirectDrawIndexedArgs
                {
                    indexCountPerInstance = lod.Mesh.GetIndexCount(0),
                    instanceCount = 0,
                    startIndex = lod.Mesh.GetIndexStart(0),
                    baseVertexIndex = lod.Mesh.GetBaseVertex(0),
                    startInstance = 0
                });
            }
        }

        return parameters.ToArray();
    }
}