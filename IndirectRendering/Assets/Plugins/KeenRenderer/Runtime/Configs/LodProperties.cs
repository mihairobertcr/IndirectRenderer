using System;
using UnityEngine;

namespace Keensight.Rendering.Configs
{
    [Serializable]
    public class LodProperties
    {
        public Mesh Mesh;
        public uint Range;
        public bool CastsShadows;
    
        public MaterialPropertyBlock MaterialPropertyBlock;

        public void Initialize()
        {
            MaterialPropertyBlock = new MaterialPropertyBlock();
        }
    }   
}