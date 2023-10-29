using System;
using UnityEngine;

public partial class MeshProperties
{
    [Serializable]
    public class TransformDto
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
    }
}