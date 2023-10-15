using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ArgumentsBuffer : IDisposable
{
    public const int ARGUMENTS_COUNT = 5;

    public int InstanceArgumentsCount { get; }
    public GraphicsBuffer GraphicsBuffer { get; }

    private readonly InstanceProperties[] _properties;
    private readonly GraphicsBuffer.IndirectDrawIndexedArgs[] _parameters;

    public ArgumentsBuffer(InstanceProperties[] properties, int lodsCount)
    {
        _properties = properties;
        _parameters = InitializeArgumentsBuffer();
        
        InstanceArgumentsCount = ARGUMENTS_COUNT * lodsCount;
        GraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, _properties.Length * lodsCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);

        Reset();
    }

    public void Reset()
    {
        GraphicsBuffer.SetData(_parameters);
    }

    public void Dispose()
    {
        GraphicsBuffer?.Dispose();
    }

    //TODO: Change to GraphicsBuffer
    public void Log(string meshesPrefix = "")
    {
        var args = new uint[InstanceArgumentsCount * _properties.Length];
        GraphicsBuffer.GetData(args);
    
        var log = new StringBuilder();
        if (!string.IsNullOrEmpty(meshesPrefix))
        {
            log.AppendLine(meshesPrefix);
        }
        
        // log.AppendLine("");
        log.AppendLine("IndexCountPerInstance InstanceCount StartIndex BaseVertexIndex StartInstance");
    
        var counter = 0;
        log.AppendLine(_properties[counter].CombinedMesh.name);
        for (var i = 0; i < args.Length; i++)
        {
            log.Append($"{args[i]} ");
            
            if ((i + 1) % ARGUMENTS_COUNT != 0) continue;
            log.AppendLine("");

            if ((i + 1) >= args.Length || (i + 1) % InstanceArgumentsCount != 0) continue;
            log.AppendLine("");

            counter++;
            var properties = _properties[counter];
            var mesh = properties.CombinedMesh;
            log.AppendLine(mesh.name);
        }
    
        Debug.Log(log.ToString());
    }

    private GraphicsBuffer.IndirectDrawIndexedArgs[] InitializeArgumentsBuffer()
    {
        var parameters = new List<GraphicsBuffer.IndirectDrawIndexedArgs>();
        foreach (var property in _properties)
        {
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