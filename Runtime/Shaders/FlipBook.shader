// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SpriteRendering/FlipBook" {
    Properties {
        _MainTex ("FlipBook", 2D) = "white" {}
        [PerInstanceRenderData] spriteRenderData ("spriteRenderData", vector) = (0,0,0,0)
    }
    
    SubShader {
        Tags{
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        
        Cull Off
        Lighting Off
		ZWrite Off
		ZTest Off
       
		Blend One OneMinusSrcAlpha
        Pass {
            CGPROGRAM


            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing	

            #include "UnityCG.cginc"
            #include "SpriteRenderingCG.cginc"

            sampler2D _MainTex;
          
UNITY_INSTANCING_BUFFER_START(Props)
     UNITY_DEFINE_INSTANCED_PROP(float4, _SpriteTransform)      
     UNITY_DEFINE_INSTANCED_PROP(float4, _Color)      
     UNITY_DEFINE_INSTANCED_PROP(float, _SpriteIndex)        
UNITY_INSTANCING_BUFFER_END(Props)

            float4 _SpriteSheetUVs[10];


            struct v2f{
                float4 vertex : SV_POSITION;
                float2 uv: TEXCOORD0;
                float4 color : color;
            };


            v2f vert (appdata_full v) {
          
                UNITY_SETUP_INSTANCE_ID(v);
                float4 renderData = UNITY_ACCESS_INSTANCED_PROP(Props, _SpriteTransform);              
                
                float spriteIndex = UNITY_ACCESS_INSTANCED_PROP(Props, _SpriteIndex);
                
                float4 uvs = _SpriteSheetUVs[spriteIndex];
                                
                v2f o;
                o.vertex = ObjectToClipPos2D(renderData, v.vertex);
                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                o.uv = uvs.xy + v.texcoord * uvs.zw;
                
             
                return o;
            }

            fixed4 frag (v2f i) : SV_Target{
              fixed4 col = tex2D(_MainTex, i.uv) * i.color;
				clip(col.a - 1.0 / 255.0);
                col.rgb *= col.a;

				return col;
            }

            ENDCG
        }
    }
}