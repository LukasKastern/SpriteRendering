using Unity.Entities;
using Unity.Mathematics;

namespace SpriteRendering {
    internal struct PropertyFloat4
    {
        public float4 DefaultValue;
        public int ShaderPropertyId;
        public ComponentType ComponentType;
    }
}