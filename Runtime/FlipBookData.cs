using System.Linq;
using UnityEngine;

namespace SpriteRendering {
    public class FlipBookData : ScriptableObject
    {
        public Sprite[] sprites = new Sprite[0];
        public Vector4[] uvs = new Vector4[0];

        public Texture2D flipBookTexture;

        private void OnValidate( ) { BakeFlipBook( ); }

        public void BakeFlipBook( )
        {
            if ( flipBookTexture == null )
            {
                flipBookTexture = new Texture2D( 1, 1, TextureFormat.DXT1, false );
            }

            var spritesToBake = sprites.Where( i => i != null ).ToArray( );

            if ( spritesToBake.Length == 0 )
            {
                flipBookTexture = null;

                return;
            }
        
            var uvsAsRect = flipBookTexture.PackTextures( spritesToBake.Select(i => i.texture).ToArray( ), 4, 2048, false );
            uvs = new Vector4[uvsAsRect.Length];
            
            for ( int i = 0; i < uvsAsRect.Length; ++i )
            {
                var rect = uvsAsRect[i];
                uvs[i] = new Vector4( rect.x, rect.y, rect.width, rect.height );
            }
        }

        public Sprite GetSprite( int sprite )
        {
            if ( sprite < 0 || sprite >= this.sprites.Length ) return null;

            return  this.sprites.Where( i => i != null ).ElementAt( sprite );
        }
        
    }
}