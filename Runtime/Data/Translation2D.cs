using Unity.Entities;
using Unity.Mathematics;

namespace SpriteRendering 
{
    public struct Translation2D : IComponentData
    {
        public float2 Value;
    }
}