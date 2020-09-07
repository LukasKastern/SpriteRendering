using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[assembly: InternalsVisibleTo("SpriteRendering.Hybrid")]

namespace SpriteRendering 
{
    // Note: We currently only support uniform scale. That way we can pass all position information as a single float4 to our shaders.

    [RegisterInstancedProperty("_SpriteTransform", 0, 0, 0, 0)]
    internal struct SpriteRenderData : IComponentData
    {
        //XY Position
        //Z Rotation
        //W Scale
        public float4 Value;
    }
}