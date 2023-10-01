using System;
using UnityEngine;
using IndirectRendering;

public class RendererDataContext : IDisposable
{
    public int MeshesCount { get; }
    public ComputeBuffer BoundingBoxes { get; }
    
    public ArgumentsBuffer Arguments { get; }
    public TransformBuffer Transforms { get; }
    public SortingBuffer Sorting { get; }
    public InstancesDataBuffer Visibility { get; }
    public InstancesDataBuffer GroupSums { get; }
    public InstancesDataBuffer ScannedPredicates { get; }
    public InstancesDataBuffer ScannedGroupSums { get; }

    public RendererDataContext(InstanceProperties[] meshProperties, int meshesCount)
    {
        MeshesCount = meshesCount;
        BoundingBoxes = new ComputeBuffer(MeshesCount, BoundsData.Size, ComputeBufferType.Default);

        Arguments = new ArgumentsBuffer(meshProperties);
        Transforms = new TransformBuffer(meshesCount);
        Sorting = new SortingBuffer(meshesCount);
        Visibility = new InstancesDataBuffer(meshesCount);
        GroupSums = new InstancesDataBuffer(meshesCount);
        ScannedPredicates = new InstancesDataBuffer(meshesCount);
        ScannedGroupSums = new InstancesDataBuffer(meshesCount);
    }

    public void Dispose()
    {
        BoundingBoxes?.Dispose();

        Arguments?.Dispose();
        Transforms?.Dispose();
        Sorting?.Dispose();
        Visibility?.Dispose();
        GroupSums?.Dispose();
        ScannedPredicates?.Dispose();
        ScannedGroupSums?.Dispose();
    }
}