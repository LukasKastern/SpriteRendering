using Unity.Entities;

namespace SpriteRendering {
    internal struct PropertyFloat
    {
        public float DefaultValue;
        public int ShaderPropertyId;
        public ComponentType ComponentType;
    }
}