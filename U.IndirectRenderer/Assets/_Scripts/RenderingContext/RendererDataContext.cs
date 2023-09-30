using System;
using UnityEngine;

public class RendererDataContext : IDisposable
{
    public int MeshesCount { get; }
    public ComputeBuffer BoundsData { get; }
    
    public ArgumentsBuffer Arguments { get; }
    public TransformBuffer Transform { get; }
    public SortingBuffer Sorting { get; }
    public InstancesDataBuffer Visibility { get; }
    public InstancesDataBuffer GroupSums { get; }
    public InstancesDataBuffer ScannedPredicates { get; }
    public InstancesDataBuffer ScannedGroupSums { get; }

    public RendererDataContext(MeshProperties[] meshProperties, int meshesCount, IndirectRendererConfig config)
    {
        MeshesCount = meshesCount;
        BoundsData = new ComputeBuffer(MeshesCount, IndirectRendering.BoundsData.Size, ComputeBufferType.Default);

        Arguments = new ArgumentsBuffer(meshProperties);
        Transform = new TransformBuffer(meshesCount);
        Sorting = new SortingBuffer(meshesCount);
        Visibility = new InstancesDataBuffer(meshesCount);
        GroupSums = new InstancesDataBuffer(meshesCount);
        ScannedPredicates = new InstancesDataBuffer(meshesCount);
        ScannedGroupSums = new InstancesDataBuffer(meshesCount);
    }

    public void Dispose()
    {
        BoundsData?.Dispose();

        Arguments?.Dispose();
        Transform?.Dispose();
        Sorting?.Dispose();
        Visibility?.Dispose();
        GroupSums?.Dispose();
        ScannedPredicates?.Dispose();
        ScannedGroupSums?.Dispose();
    }
}