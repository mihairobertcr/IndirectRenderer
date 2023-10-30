using System;
using System.Text;
using UnityEngine;
using Keensight.Rendering.Data;

namespace Keensight.Rendering.Context
{
    public class SortingBuffer : IDisposable
    {
        public ComputeBuffer Data { get; }
        public ComputeBuffer Temp { get; }

        private readonly int _count;

        public SortingBuffer(int count)
        {
            Data = new ComputeBuffer(count, SortingData.Size, ComputeBufferType.Default);
            Temp = new ComputeBuffer(count, SortingData.Size, ComputeBufferType.Default);
            _count = count;
        }

        public void Dispose()
        {
            Data?.Dispose();
            Temp?.Dispose();
        }
        
        public void Log(string prefix = "")
        {
            var data = new SortingData[_count];
            Data.GetData(data);
            
            var log = new StringBuilder();
            if (!string.IsNullOrEmpty(prefix))
            {
                log.AppendLine(prefix);
            }
            
            var lastDrawCallIndex = 0u;
            for (var i = 0; i < data.Length; i++)
            {
                var drawCallIndex = (data[i].DrawCallInstanceIndex >> 16);
                var instanceIndex = (data[i].DrawCallInstanceIndex) & 0xFFFF;
                if (i == 0)
                {
                    lastDrawCallIndex = drawCallIndex;
                }
                
                log.AppendLine($"({drawCallIndex}) --> {data[i].DistanceToCamera} instanceIndex: {instanceIndex}");

                if (lastDrawCallIndex == drawCallIndex) continue;
                
                Debug.Log(log.ToString());
                log = new StringBuilder();
                lastDrawCallIndex = drawCallIndex;
            }

            Debug.Log(log.ToString());
        }
    }
}
