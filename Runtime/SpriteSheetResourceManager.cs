using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SpriteRendering 
{
    public class SpriteSheetResourceManager : SystemBase
    {
        private Dictionary<FlipBookData, Material> s_FlipBookMaterials = new Dictionary<FlipBookData, Material>( );
        private HashSet<Material> m_initializedMaterials = new HashSet<Material>( );
        private Dictionary<float2, Mesh> s_SizeToMesh = new Dictionary<float2, Mesh>();
    
        private Dictionary<Texture2D, Material> m_defaultSingleSpriteMaterials = new Dictionary<Texture2D, Material>();

        public static readonly int UVs = Shader.PropertyToID( "_SpriteSheetUVs" );
        public static readonly int SpriteSheet = Shader.PropertyToID( "_MainTex" );
        public static readonly int Sprite = Shader.PropertyToID( "_MainTex" );

        private static readonly Shader DefaultSpriteSheetShader = Shader.Find( "SpriteRendering/FlipBook" );
        private static readonly Shader DefaultSingleSpriteShader = Shader.Find( "SpriteRendering/Default" );

        private void InitializeFlipBook( Material materialToInitialize, FlipBookData fipBook )
        {
            fipBook.BakeFlipBook( );
            
            //Debug.Log(fipBook.flipBookTexture);
            materialToInitialize.SetVectorArray( UVs, fipBook.uvs );
            materialToInitialize.SetTexture( SpriteSheet, fipBook.flipBookTexture );

            materialToInitialize.enableInstancing = true;
            m_initializedMaterials.Add( materialToInitialize );
        }

        private void InitializeSingleSpriteMaterial( Material materialToInitialize, Texture2D sprite )
        {
            materialToInitialize.SetTexture( Sprite, sprite );
            
            materialToInitialize.enableInstancing = true;
            m_initializedMaterials.Add( materialToInitialize );
        }
        
        private static Mesh CreateMesh(float width, float height)
        {
            var mesh = new Mesh( );
            
            var vertices = new Vector3[]
            {
                new Vector3( -width / 2, -height / 2, 0 ),
                new Vector3( width / 2, -height / 2, 0 ),
                new Vector3( -width / 2, height / 2, 0 ),
                new Vector3( width / 2, height / 2, 0 )
            };
        
            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
                // lower left triangle
                0, 2, 1,
                // upper right triangle
                2, 3, 1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            mesh.uv = uv;
        
            mesh.RecalculateNormals( );
         
            return mesh;
        }
    
        public RenderSprite GetSingleSpriteReference( Texture2D texture2D, [CanBeNull] Material material, float2 planeSize  )
        {

            if ( !s_SizeToMesh.TryGetValue( planeSize, out var mesh ) )
            {
                mesh = CreateMesh( planeSize.x, planeSize.y );
                s_SizeToMesh.Add( planeSize, mesh );
            }
        
            if ( texture2D == null ) throw new ArgumentException( "Texture cannot be null" );

            if ( material == null )
            {
                if ( !m_defaultSingleSpriteMaterials.TryGetValue( texture2D, out material ) )
                {
                    material = new Material( DefaultSingleSpriteShader );
                    
                    material.enableInstancing = true;
                    
                    InitializeSingleSpriteMaterial( material, texture2D );
            
                    m_defaultSingleSpriteMaterials.Add( texture2D, material );   
                }
            }

            if ( !m_initializedMaterials.Contains( material ) ) InitializeSingleSpriteMaterial( material, texture2D );
        
            return new RenderSprite( )
            {
                material = material,
                mesh = mesh,
            };
        }
    
        public RenderSprite GetFlipBookReference( FlipBookData flipBook, [CanBeNull] Material material, float2 planeSize  )
        {
            if ( !s_SizeToMesh.TryGetValue( planeSize, out var mesh ) )
            {
                mesh = CreateMesh( planeSize.x, planeSize.y );
                s_SizeToMesh.Add( planeSize, mesh );
            }
        
            if ( flipBook == null ) 
                throw new ArgumentException( "Flip book cannot be null" );
            
            
            
            if ( material == null )
            {
                if ( !s_FlipBookMaterials.TryGetValue( flipBook, out material ) )
                {
                    material = new Material( DefaultSpriteSheetShader );
                    InitializeFlipBook( material, flipBook );

                    s_FlipBookMaterials.Add( flipBook, material );   
                }
            }
            
            

            if ( !m_initializedMaterials.Contains( material ) ) InitializeFlipBook( material, flipBook );
        
            return new RenderSprite( )
            {
                material = material,
                mesh = mesh
            };
        }

        protected override void OnUpdate( ) {  }
    }
}

