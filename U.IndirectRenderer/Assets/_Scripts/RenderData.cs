using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace IndirectRendering
{
    // Preferably want to have all buffer structs in power of 2...
    // 6 * 4 bytes = 24 bytes
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct BoundsData
    {
        public Vector3 BoundsCenter;
        public Vector3 BoundsExtents;

        public static int Size =>
            sizeof(float) * 3 + 
            sizeof(float) * 3;
    }

    // 8 * 4 bytes = 32 bytes
    [StructLayout(LayoutKind.Sequential)]
    public struct Indirect2x2Matrix
    {
        public Vector4 FirstRow;
        public Vector4 SecondRow;
    
        public static int Size =>
            sizeof(float) * 4 + 
            sizeof(float) * 4;
    };

    // 2 * 4 bytes = 8 bytes
    [StructLayout(LayoutKind.Sequential)]
    public struct SortingData
    {
        public uint  DrawCallInstanceIndex;
        public float DistanceToCamera;
    
        public static int Size =>
            sizeof(uint) + 
            sizeof(float);
    };
}

