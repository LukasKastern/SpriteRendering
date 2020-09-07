using Unity.Entities;

namespace SpriteRendering 
{
    public struct Rotation2D : IComponentData
    {
        /// <summary>
        /// Rotation in Radians
        /// </summary>
        public float Value;
    }
}