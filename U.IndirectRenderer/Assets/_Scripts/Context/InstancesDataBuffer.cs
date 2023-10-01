using System;
using System.Text;
using UnityEngine;

public class InstancesDataBuffer : IDisposable
{
    public ComputeBuffer Meshes { get; }
    public ComputeBuffer Shadows { get; }

    private readonly int _count;
    
    public InstancesDataBuffer(int count)
    {
        Meshes = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Default);
        Shadows = new ComputeBuffer(count, sizeof(uint), ComputeBufferType.Default);
        _count = count;
    }

    public void Dispose()
    {
        Meshes?.Dispose();
        Shadows?.Dispose();
    }
    
    public void Log(string meshPrefix = "", string shadowPrefix = "")
    {
        var meshesData = new uint[_count];
        var shadowsData = new uint[_count];
        
        Meshes.GetData(meshesData);
        Shadows.GetData(shadowsData);
        
        var meshesLog = new StringBuilder();
        var shadowsLog = new StringBuilder();

        if (!string.IsNullOrEmpty(meshPrefix))
        {
            meshesLog.AppendLine(meshPrefix);
        }

        if (!string.IsNullOrEmpty(shadowPrefix))
        {
            shadowsLog.AppendLine(shadowPrefix);
        }
        
        for (var i = 0; i < meshesData.Length; i++)
        {
            meshesLog.AppendLine(i + ": " + meshesData[i]);
            shadowsLog.AppendLine(i + ": " + shadowsData[i]);
        }

        Debug.Log(meshesLog.ToString());
        Debug.Log(shadowsLog.ToString());
    }
}