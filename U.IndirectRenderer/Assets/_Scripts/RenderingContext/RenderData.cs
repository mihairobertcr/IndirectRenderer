using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace IndirectRendering
{
    // Preferably want to have all buffer structs in power of 2...
    // 8 * 4 bytes = 32 bytes
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix2x2
    {
        public Vector4 Row0;
        public Vector4 Row1;
    
        public static int Size =>
            sizeof(float) * 4 + 
            sizeof(float) * 4;
    };
    
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

