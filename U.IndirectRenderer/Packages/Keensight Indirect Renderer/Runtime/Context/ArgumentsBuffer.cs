using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Keensight.Rendering.Configs;

namespace Keensight.Rendering.Context
{
    public class ArgumentsBuffer : IDisposable
    {
        public const int ARGUMENTS_COUNT = 5;

        public int MeshArgumentsCount { get; }
        public GraphicsBuffer GraphicsBuffer { get; }

        private readonly List<MeshProperties> _properties;
        private readonly GraphicsBuffer.IndirectDrawIndexedArgs[] _arguments;

        public ArgumentsBuffer(List<MeshProperties> properties, int lodsCount)
        {
            _properties = properties;
            _arguments = InitializeArgumentsBuffer();
            
            MeshArgumentsCount = ARGUMENTS_COUNT * lodsCount;
            GraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 
                _properties.Count * lodsCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);

            Reset();
        }

        public void Reset() => GraphicsBuffer.SetData(_arguments);

        public void Dispose() => GraphicsBuffer?.Dispose();

        public void Log(string meshesPrefix = "")
        {
            var args = new uint[MeshArgumentsCount * _properties.Count];
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

                if ((i + 1) >= args.Length || (i + 1) % MeshArgumentsCount != 0) continue;
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
}