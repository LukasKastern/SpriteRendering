using Unity.Entities;
using UnityEngine;

namespace SpriteRendering 
{
    [RegisterInstancedProperty("_Color", 1, 1, 1, 1)]
    public struct SpriteColor : IComponentData
    {
        public Color Value;
    }
}