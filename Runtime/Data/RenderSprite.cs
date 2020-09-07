using System;
using Unity.Entities;
using UnityEngine;

namespace SpriteRendering {
    public struct RenderSprite : ISharedComponentData, IEquatable<RenderSprite>
    {
        public Material material;
    
        public Mesh mesh;
    
        /// <summary>
        /// Higher means drawn later.
        /// </summary>
        public int sortOrder;


        public bool Equals( RenderSprite other ) => Equals( material, other.material ) && Equals( mesh, other.mesh ) && sortOrder == other.sortOrder;

        public override bool Equals( object obj ) => obj is RenderSprite other && Equals( other );

        public override int GetHashCode( )
        {
            unchecked
            {
                var hashCode = ( material != null ? material.GetHashCode( ) : 0 );
                hashCode = ( hashCode * 397 ) ^ ( mesh != null ? mesh.GetHashCode( ) : 0 );
                hashCode = ( hashCode * 397 ) ^ sortOrder;
                return hashCode;
            }
        }
    }
}