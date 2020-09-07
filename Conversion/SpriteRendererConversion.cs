using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SpriteRendering
{
    [UpdateInGroup(typeof( GameObjectConversionGroup ))]
    public class SpriteRendererConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate( )
        {
            var resourceManager = DstEntityManager.World.GetExistingSystem<SpriteSheetResourceManager>( );
            
            Entities.ForEach( ( Entity ent, Transform transform, SpriteRenderer spriteRenderer ) =>
            {
                var entity = GetPrimaryEntity( spriteRenderer );

                RenderSprite renderSprite = default;

                var spriteSize = spriteRenderer.size;
                float uniformScale = 1f;

                if ( transform.localScale.x != transform.localScale.y )
                    spriteSize *= new Vector2( transform.localScale.x, transform.localScale.y );
                else
                    uniformScale = transform.localScale.x;
                
                var material = spriteRenderer.sharedMaterial;

                if ( material == null || material.shader.name == "Sprites/Default" ) 
                    material = null;
                
                if ( spriteRenderer.sprite != null && spriteRenderer.sprite.texture != null )
                {
                    if ( !EntityManager.HasComponent<FlipBook>( ent ) )
                    {
                        renderSprite = resourceManager.GetSingleSpriteReference( spriteRenderer.sprite.texture, material, spriteSize );
                    }

                    else
                    {
                        var flipBook = EntityManager.GetComponentObject<FlipBook>( ent );
                        DeclareAssetDependency( transform.gameObject, flipBook.flipBookData );

                        renderSprite = resourceManager.GetFlipBookReference( flipBook.flipBookData, material, spriteSize );

                        DstEntityManager.AddComponentData( entity, new SpriteIndex( )
                        {
                            Value = flipBook.currentIdx
                        } );
                    }
                }

                renderSprite.sortOrder = spriteRenderer.sortingOrder;
                
                DstEntityManager.AddSharedComponentData( entity, renderSprite );
                DstEntityManager.AddComponentData( entity, new Translation2D( )
                {
                    Value = new float2( transform.position.x, transform.position.y )
                } );

                DstEntityManager.AddComponentData( entity, new Rotation2D( )
                {
                    Value = math.radians(transform.rotation.eulerAngles.z)
                } );

                DstEntityManager.AddComponentData( entity, new UniformScale( )
                {
                    Value = uniformScale
                } );

                DstEntityManager.AddComponentData( entity, new SpriteColor( )
                {
                    Value = spriteRenderer.color
                } );
                
                DstEntityManager.AddComponent<SpriteRenderData>( entity );
            } );
        }
    }
}