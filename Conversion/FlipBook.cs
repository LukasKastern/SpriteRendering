using System;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

#if UNITY_EDITOR
#endif

namespace SpriteRendering 
{
    [RequireComponent(typeof ( SpriteRenderer ) )]
    public class FlipBook : MonoBehaviour
    {
        public FlipBookData flipBookData;
        public int currentIdx;
    }

    [CustomEditor(typeof(FlipBook))]
    public class FlipBookEditor : Editor
    {
        public override void OnInspectorGUI( )
        {
            var flipBook = this.serializedObject.targetObject as FlipBook;

            var prefabStage = PrefabStageUtility.GetPrefabStage( flipBook.gameObject );
            if ( prefabStage == null )
            {
                EditorGUILayout.HelpBox( "Please open the prefab to edit the FlipBook", MessageType.Error );
            }

            else
            {
                var flipBookProperty = this.serializedObject.FindProperty( "flipBookData" );
            
                using ( new EditorGUILayout.HorizontalScope( ) )
                {
                    EditorGUILayout.PropertyField( flipBookProperty );

                    if ( GUILayout.Button( "New FlipBook", GUILayout.Width( 150 ) ) )
                    {
                        var assetPath = prefabStage.assetPath;

                        assetPath = assetPath.Replace( ".prefab", "" );
                    
                        var flipBookData = CreateInstance<FlipBookData>( );
                        AssetDatabase.CreateAsset( flipBookData, $"{assetPath}_FlipBook.asset" );
                        flipBookProperty.objectReferenceValue = flipBookData;
                    }
                }
            
                this.serializedObject.ApplyModifiedProperties( );   
            }

            var spriteRenderer = flipBook.GetComponent<SpriteRenderer>( ); 
            if ( spriteRenderer == null ) spriteRenderer = flipBook.gameObject.AddComponent<SpriteRenderer>( );
            
            if ( flipBook.flipBookData == null ) return;
            
            
            var currentIdx = flipBook.currentIdx;

            EditorGUI.BeginChangeCheck( );
            currentIdx = EditorGUILayout.IntSlider( currentIdx, 0, flipBook.flipBookData.sprites.Where(i => i != null && i).Count() - 1 );
            
            
            if ( EditorGUI.EndChangeCheck( ) )
            {
                flipBook.currentIdx = currentIdx;
                EditorUtility.SetDirty( flipBook );
                
                var sprite = flipBook.flipBookData.GetSprite( flipBook.currentIdx );
                spriteRenderer.sprite = sprite;
            }
            

        }
    }
    
}