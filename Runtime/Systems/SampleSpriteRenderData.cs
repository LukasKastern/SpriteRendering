using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SpriteRendering 
{
    [UpdateInGroup(typeof( PresentationSystemGroup ))]
    [UpdateBefore(typeof( SpriteRenderSystem ))]
    public class SampleSpriteRenderData : SystemBase
    {
        protected override void OnUpdate( )
        {
            Entities.WithChangeFilter<Translation2D>( ).ForEach( ( ref SpriteRenderData renderData, in Translation2D translation2D ) =>
            {
                renderData.Value.xy = translation2D.Value;
            } ).Schedule( );

            Entities.WithChangeFilter<Rotation2D>( ).ForEach( ( ref SpriteRenderData renderData, in Rotation2D rotation ) =>
            {
                renderData.Value.z = rotation.Value;
            } ).Schedule( );
            
            Entities.WithChangeFilter<UniformScale>( ).ForEach( ( ref SpriteRenderData renderData, in UniformScale scale2D ) =>
            {
                renderData.Value.w = scale2D.Value;
            } ).Schedule( );
        }
    }
}