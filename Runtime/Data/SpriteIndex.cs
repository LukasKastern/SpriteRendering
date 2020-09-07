using Unity.Entities;

namespace SpriteRendering 
{
    [RegisterInstancedProperty("_SpriteIndex", 0)]
    public struct SpriteIndex : IComponentData
    {
        public float Value;
    }
}