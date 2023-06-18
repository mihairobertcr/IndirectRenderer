using System;
using UnityEngine;
using UnityEngine.Rendering;

public static class ShaderBuffers
{
    public static ComputeBuffer InstancesArgsBuffer;
    
    public static ComputeBuffer InstanceMatrixRows01;
    public static ComputeBuffer InstanceMatrixRows23;
    public static ComputeBuffer InstanceMatrixRows45;

    public static ComputeBuffer InstancesSortingData;
    public static ComputeBuffer InstancesSortingDataTemp;
    
    public static CommandBuffer SortingCommandBuffer;
    
    public static void Dispose()
    {
        InstancesArgsBuffer.Release();
        
        InstanceMatrixRows01.Release();
        InstanceMatrixRows23.Release();
        InstanceMatrixRows45.Release();
        
        InstancesSortingData.Release();
        InstancesSortingDataTemp.Release();
        
        SortingCommandBuffer.Release();
    }
}
