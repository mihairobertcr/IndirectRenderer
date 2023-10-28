using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using IndirectRendering;

public class RendererDataContext : IDisposable
{
    public int MeshesCount { get; }
    public int LodsCount { get; }
    
    public ArgumentsBuffer Arguments { get; }
    public TransformBuffer Transforms { get; }
    public SortingBuffer Sorting { get; }
    
    public ComputeBuffer LodsRanges { get; }
    public ComputeBuffer DefaultLods { get; }
    public ComputeBuffer BoundingBoxes { get; }
    public ComputeBuffer Visibility { get; }
    public ComputeBuffer GroupSums { get; }
    public ComputeBuffer ScannedPredicates { get; }
    public ComputeBuffer ScannedGroupSums { get; }

    public RendererDataContext(RendererConfig config, List<InstanceProperties> meshProperties)
    {
        MeshesCount = (int)config.NumberOfInstances * meshProperties.Count;
        LodsCount = config.NumberOfLods;
        
        Arguments = new ArgumentsBuffer(meshProperties, config.NumberOfLods);
        Transforms = new TransformBuffer(MeshesCount);
        Sorting = new SortingBuffer(MeshesCount);
        
        LodsRanges = new ComputeBuffer(meshProperties.Count * config.NumberOfLods, sizeof(float), ComputeBufferType.Default);
        DefaultLods = new ComputeBuffer(meshProperties.Count, sizeof(uint), ComputeBufferType.Default);
        BoundingBoxes = new ComputeBuffer(MeshesCount, BoundsData.Size, ComputeBufferType.Default);
        Visibility = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        GroupSums = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        ScannedPredicates = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
        ScannedGroupSums = new ComputeBuffer(MeshesCount, sizeof(uint), ComputeBufferType.Default);
    }

    public void Dispose()
    {
        Arguments?.Dispose();
        Transforms?.Dispose();
        Sorting?.Dispose();
        
        LodsRanges?.Dispose();
        DefaultLods?.Dispose();
        BoundingBoxes?.Dispose();
        Visibility?.Dispose();
        GroupSums?.Dispose();
        ScannedPredicates?.Dispose();
        ScannedGroupSums?.Dispose();
    }

    public void LogVisibility(string prefix = "") => Log(Visibility, prefix);
    public void LogGroupSums(string prefix = "") => Log(GroupSums, prefix);
    public void LogScannedPredicates(string prefix = "") => Log(ScannedPredicates, prefix);
    public void LogScannedGroupSums(string prefix = "") => Log(ScannedGroupSums, prefix);

    private void Log(ComputeBuffer buffer, string prefix = "")
    {
        var instances = new uint[MeshesCount];
        buffer.GetData(instances);
        
        var meshesLog = new StringBuilder();
        if (!string.IsNullOrEmpty(prefix))
        {
            meshesLog.AppendLine(prefix);
        }

        for (var i = 0; i < instances.Length; i++)
        {
            meshesLog.AppendLine(i + ": " + instances[i]);
        }

        Debug.Log(meshesLog.ToString());
    }
}