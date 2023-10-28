using System;
using UnityEngine;

public partial class InstanceProperties
{
    [Serializable]
    public class TransformDto
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
    }
}