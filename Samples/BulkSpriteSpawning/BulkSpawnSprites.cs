using System;
using System.Collections;
using System.Collections.Generic;
using SpriteRendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = System.Random;

public class BulkSpawnSprites : MonoBehaviour
{
    public bool spawnSprites;
    public Transform spritePrefab;
    public Rect boundsToSpawnSpritesIN;
    public int spriteCountPerDimension;
    
    public bool useUnitySpriteRendering;
    
    private Entity m_entityPrefab;
    

    private void Start( )
    {
        if ( spritePrefab == null )
        {
            m_entityPrefab = Entity.Null;
            return;
        }
        
        DefaultWorldInitialization.DefaultLazyEditModeInitialize( );

        var settings = GameObjectConversionSettings.FromWorld( World.DefaultGameObjectInjectionWorld, null );
        m_entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy( spritePrefab.gameObject, settings );
    }

    private void Update( )
    {
        if ( !spawnSprites || m_entityPrefab == Entity.Null ) return;
        
        if ( spawnSprites )
            spawnSprites = false;

        
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        var entityManager = defaultWorld.EntityManager;

        var xCount = spriteCountPerDimension;
        var yCount = spriteCountPerDimension;

        if ( !useUnitySpriteRendering )
        {
            var entities = new NativeArray<Entity>( spriteCountPerDimension * spriteCountPerDimension, Allocator.Temp );
            entityManager.Instantiate( m_entityPrefab, entities );

            for ( int y = 0; y < yCount; ++y )
            {
                for ( int x = 0; x < xCount; ++x )
                {
                    var yPosition = math.lerp( boundsToSpawnSpritesIN.yMin, boundsToSpawnSpritesIN.yMax, (float)y / yCount );
                    var xPosition = math.lerp( boundsToSpawnSpritesIN.xMin, boundsToSpawnSpritesIN.xMax, (float)x / xCount );

                    entityManager.SetComponentData( entities[y * xCount + x], new Translation2D( )
                    {
                        Value = new float2( xPosition + UnityEngine.Random.Range(-3f, 3f), yPosition + UnityEngine.Random.Range(-3f, 3f) )
                    } );

                    entityManager.SetComponentData( entities[y * xCount + x], new Rotation2D( )
                    {
                        Value = math.radians( UnityEngine.Random.Range( -360, 360 ) )
                    } );
                }
            }   
        }

        else
        {
            for ( int y = 0; y < yCount; ++y )
            {
                for ( int x = 0; x < xCount; ++x )
                {
                    var yPosition = math.lerp( boundsToSpawnSpritesIN.yMin, boundsToSpawnSpritesIN.yMax, (float)y / yCount );
                    var xPosition = math.lerp( boundsToSpawnSpritesIN.xMin, boundsToSpawnSpritesIN.xMax, (float)x / xCount );


                    var position = new float2( xPosition + UnityEngine.Random.Range( -3f, 3f ),
                    yPosition + UnityEngine.Random.Range( -3f, 3f ) );
                    var rotation =  UnityEngine.Random.Range( -360, 360 );

                    var spriteRenderer = GameObject.Instantiate( spritePrefab );
                    spriteRenderer.transform.position = new Vector3( position.x, position.y );
                    spriteRenderer.transform.rotation = Quaternion.Euler( 0, 0, rotation );

                    GameObject.Destroy( spriteRenderer.GetComponent<ConvertToEntity>( ) );
                }
            }      
        }
    }
}
