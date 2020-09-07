

            float4x4 rotationZMatrix(float rotation){
                float c = cos(rotation);
                float s = sin(rotation);
                float4x4 ZMatrix  = 
                    float4x4( 
                       c,  s, 0,  0,
                       -s,  c,  0,  0,
                       0,  0,  1,  0,
                       0,  0,  0,  1);
                return ZMatrix;
            }
            
float4 ObjectToClipPos2D(float4 renderData, float2 vertex)
{
                float2 spritePosition = renderData.xy;
                float rotation = renderData.z;
                float scale = renderData.w;

                //rotate the vertex
                vertex = mul(vertex * scale, rotationZMatrix(rotation));
                
                return mul(UNITY_MATRIX_VP, float4(vertex + spritePosition, 0, 1));
}