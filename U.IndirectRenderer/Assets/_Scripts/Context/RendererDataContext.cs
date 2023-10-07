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

    public RendererDataContext(IndirectRendererConfig config, InstanceProperties[] meshProperties)
    {
        MeshesCount = (int)config.NumberOfInstances * meshProperties.Length;
        BoundingBoxes = new ComputeBuffer(MeshesCount, BoundsData.Size, ComputeBufferType.Default);

        Arguments = new ArgumentsBuffer(meshProperties, config.NumberOfLods);
        Transforms = new TransformBuffer(MeshesCount);
        Sorting = new SortingBuffer(MeshesCount);
        Visibility = new InstancesDataBuffer(MeshesCount);
        GroupSums = new InstancesDataBuffer(MeshesCount);
        ScannedPredicates = new InstancesDataBuffer(MeshesCount);
        ScannedGroupSums = new InstancesDataBuffer(MeshesCount);
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