// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Pixel Art/Indexing" {
 Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", COLOR) = (1,1,1,1)
        _LUTTex ("Texture", 2D) = "white" {} 
        _LUTSize ("LUT Size", float) = 16
    }
    
    SubShader {
            Tags { "RenderType"="Transparent"  "Queue" = "Transparent" }
            Zwrite Off
            ZTest Always            
            Pass{
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag 
                #pragma fragmentoption ARB_precision_hint_fastest            
                #include "UnityCG.cginc"

                uniform sampler2D _MainTex;
                uniform float4 _MainTex_ST;
                uniform sampler2D _LUTTex;
              	uniform float _LUTSize;
              	float4 _Color;
                struct a2v  {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                };
 
                struct v2f  {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;       // Made this a float, fixed sometimes isn't precise enough for UVs.
                };
 
                v2f vert(a2v  i){
                    v2f o; 
                    o.pos = UnityObjectToClipPos(i.vertex);
                    o.uv = TRANSFORM_TEX (i.texcoord, _MainTex);
                    return o;
                } 
                
				float4 sampleAs3DTexture(sampler2D tex, float3 uv, float width) {
				float innerWidth = width - 1.0;
				float sliceSize = 1.0 / width; // space of 1 slice
				float slicePixelSize = sliceSize / width; // space of 1 pixel
				float sliceInnerSize = slicePixelSize * innerWidth; // space of width pixels
				//float zSlice0 = min(floor(uv.z * innerWidth), innerWidth);
				float zSlice1 = min(floor(uv.z * innerWidth) + 1.0, innerWidth);
				float xOffset = slicePixelSize * 0.5 + uv.x * sliceInnerSize;
				float s1 = xOffset + (zSlice1 * sliceSize);
				float4 sliceColor = tex2Dbias(tex, float4(s1, uv.y,0,0));
				float4 result = sliceColor;
				return result;
				}
				
				
//				vec4 sampleAs3DTexture(sampler2D texture, vec3 uv, float width) {
//				float sliceSize = 1.0 / width;              // space of 1 slice
//				float slicePixelSize = sliceSize / width;           // space of 1 pixel
//				float sliceInnerSize = slicePixelSize * (width - 1.0);  // space of width pixels
//				float zSlice0 = min(floor(uv.z * width), width - 1.0);
//				float zSlice1 = min(zSlice0 + 1.0, width - 1.0);
//				float xOffset = slicePixelSize * 0.5 + uv.x * sliceInnerSize;
//				float s0 = xOffset + (zSlice0 * sliceSize);
//				float s1 = xOffset + (zSlice1 * sliceSize);
//				vec4 slice0Color = texture2D(texture, vec2(s0, uv.y));
//				vec4 sliceColor = texture2D(texture, vec2(s1, uv.y));
//				float zOffset = mod(uv.z * width, 1.0);
//				vec4 result = mix(slice0Color, sliceColor, zOffset);
//				return result;			
				
				
				
				
				
				
                // Made this a fixed4, you won't gain precision with a float.
                fixed4 frag(v2f i) : COLOR
                {
                    float3 scale = float3((_LUTSize - 1.0),(_LUTSize - 1.0),(_LUTSize - 1.0)) / _LUTSize;
					float3 offset = float3(1.0,1.0,1.0) / (2.0 * _LUTSize);
                    fixed4 c;
                    fixed3 source = tex2D(_MainTex, i.uv).rgb;
                    source.rgb -= (1-_Color.rgb);
                    //source.rgb *= (_Color.rgb/2)+.5;
                    source.rgb = saturate(source.rgb);
                    c.rgb = sampleAs3DTexture(_LUTTex, scale*source+offset,_LUTSize);
                    c.a = 1;
                    return c;
                    
                    

                }
            ENDCG
        }    
		
	}
    Fallback "VertexLit"
}


//uniform sampler2D TextureMap;
//uniform sampler3D ColorGradingLUT;
//varying vec2 f_texCoord;
//const float lutSize = 16.0;
//const vec3 scale = vec3((lutSize - 1.0) / lutSize);
//const vec3 offset = vec3(1.0 / (2.0 * lutSize));
//void main()
//{
// vec4 rawColor = texture2D(TextureMap, f_texCoord);
// gl_FragColor = texture3D(ColorGradingLUT, scale * rawColor.xyz + offset);
//}



